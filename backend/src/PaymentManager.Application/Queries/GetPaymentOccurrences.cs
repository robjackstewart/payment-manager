using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Enums;
using static PaymentManager.Application.Queries.GetPaymentOccurrences;
using static PaymentManager.Application.Queries.GetPaymentOccurrences.Response;

namespace PaymentManager.Application.Queries;

public record GetPaymentOccurrences(Guid UserId, DateOnly From, DateOnly To) : IRequest<Response>
{
    internal sealed class Handler(IReadOnlyPaymentManagerContext context, ILogger<Handler> logger)
        : IRequestHandler<GetPaymentOccurrences, Response>
    {
        public async Task<Response> Handle(GetPaymentOccurrences request, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Fetching payment occurrences for user '{UserId}' between {From} and {To}",
                request.UserId, request.From, request.To);

            var payments = await context.Payments
                .Where(x => x.UserId == request.UserId)
                .ToArrayAsync(cancellationToken);

            var paymentIds = payments.Select(p => p.Id).ToHashSet();

            var splitRows = await context.PaymentSplits
                .Where(s => paymentIds.Contains(s.PaymentId))
                .Select(s => new { s.PaymentId, s.PersonId, s.Percentage })
                .ToArrayAsync(cancellationToken);

            var splitRowsByPayment = splitRows
                .GroupBy(s => s.PaymentId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var effectiveSplitRows = await context.EffectivePaymentSplits
                .Where(s => paymentIds.Contains(s.PaymentId))
                .OrderBy(s => s.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var effectiveSplitsByPayment = effectiveSplitRows
                .GroupBy(s => s.PaymentId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var effectiveValueRows = await context.EffectivePaymentValues
                .Where(v => paymentIds.Contains(v.PaymentId))
                .OrderBy(v => v.EffectiveDate)
                .ToArrayAsync(cancellationToken);

            var effectiveValuesByPayment = effectiveValueRows
                .GroupBy(v => v.PaymentId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var groupIds = payments
                .Where(p => p.PayerGroupId.HasValue)
                .Select(p => p.PayerGroupId!.Value)
                .ToHashSet();

            var memberRows = await context.PayerGroupMembers
                .Where(m => groupIds.Contains(m.PayerGroupId))
                .Select(m => new { m.PayerGroupId, m.PersonId })
                .ToArrayAsync(cancellationToken);

            var membersByGroup = memberRows
                .GroupBy(m => m.PayerGroupId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.PersonId).ToHashSet());

            var occurrences = payments
                .SelectMany(p =>
                {
                    var rows = splitRowsByPayment.GetValueOrDefault(p.Id) ?? [];
                    var effectiveSplits = effectiveSplitsByPayment.GetValueOrDefault(p.Id) ?? [];
                    var effectiveValues = effectiveValuesByPayment.GetValueOrDefault(p.Id) ?? [];
                    var initialSplits = rows.Select(s => (s.PersonId, s.Percentage)).ToArray();
                    return PaymentOccurrenceCalculator
                        .GetOccurrences(p.Frequency, p.StartDate, p.EndDate, request.From, request.To)
                        .Select(date =>
                        {
                            var amount = EffectiveValueResolver.Resolve(effectiveValues, date, p.InitialAmount);
                            var splitSet = EffectiveSplitResolver.Resolve(initialSplits, effectiveSplits, date);
                            var splitDtos = (ICollection<OccurrenceDto.SplitDto>)SplitPaymentCalculator
                                .AllocateValues(amount, splitSet)
                                .Select(s => new OccurrenceDto.SplitDto(s.PersonId, s.Percentage, s.Value))
                                .ToArray();
                            return new OccurrenceDto(
                                p.Id, p.PaymentSourceId, p.PayeeId,
                                amount, p.Currency, p.Frequency, p.Direction,
                                date, p.StartDate, p.EndDate, p.Description, p.PayerGroupId,
                                splitDtos);
                        });
                })
                .OrderBy(o => o.OccurrenceDate)
                .ThenBy(o => o.PaymentId)
                .ToArray();

            var incomingOccurrences = occurrences.Where(o => o.Direction == PaymentDirection.Incoming).ToArray();

            // Only real payer groups get a summary entry. Ungrouped payments are not a group —
            // their per-person shares surface through the person commitments below.
            var summary = occurrences
                .Where(o => o.PayerGroupId.HasValue)
                .GroupBy(o => o.PayerGroupId!.Value)
                .OrderBy(g => g.Key)
                .Select(groupOccurrences => new GroupSummaryDto(
                    groupOccurrences.Key,
                    [.. groupOccurrences
                        .GroupBy(o => o.Currency)
                        .Select(currencyGroup =>
                        {
                            var outgoing = BuildDirectionTotals(currencyGroup.Where(o => o.Direction == PaymentDirection.Outgoing));
                            var incoming = BuildMemberIncomeTotals(
                                incomingOccurrences.Where(o => o.Currency == currencyGroup.Key),
                                membersByGroup.GetValueOrDefault(groupOccurrences.Key) ?? []);
                            var outgoingByPayee = BuildPayeeTotals(currencyGroup.Where(o => o.Direction == PaymentDirection.Outgoing));
                            return new CurrencySummaryDto(
                                currencyGroup.Key, outgoing, incoming, BuildNetTotals(incoming, outgoing), outgoingByPayee);
                        })
                        .OrderBy(c => c.Currency)]))
                .ToArray();

            var people = occurrences
                .SelectMany(o => o.Splits.Select(s => (o.Currency, o.Direction, s.PersonId, s.Value)))
                .GroupBy(x => (x.PersonId, x.Currency))
                .Select(g =>
                {
                    var income = g.Where(x => x.Direction == PaymentDirection.Incoming).Sum(x => x.Value);
                    var committed = g.Where(x => x.Direction == PaymentDirection.Outgoing).Sum(x => x.Value);
                    return new PersonCommitmentDto(g.Key.PersonId, g.Key.Currency, income, committed, income - committed, committed > income);
                })
                .OrderBy(p => p.Currency)
                .ThenBy(p => p.PersonId)
                .ToArray();

            logger.LogInformation(
                "Found {Count} occurrences for user '{UserId}' between {From} and {To}",
                occurrences.Length, request.UserId, request.From, request.To);

            return new Response([.. occurrences], [.. summary], [.. people]);
        }

        private static DirectionTotalsDto BuildDirectionTotals(IEnumerable<OccurrenceDto> occurrences)
        {
            var occurrenceArray = occurrences.ToArray();
            return new DirectionTotalsDto(
                occurrenceArray.Sum(o => o.Amount),
                [.. occurrenceArray
                    .SelectMany(o => o.Splits)
                    .GroupBy(s => s.PersonId)
                    .Select(g => new PersonAmountDto(g.Key, g.Sum(s => s.Value)))
                    .OrderBy(c => c.PersonId)],
                [.. occurrenceArray
                    .GroupBy(o => o.PaymentSourceId)
                    .Select(psGroup => new PaymentSourceBreakdownDto(
                        psGroup.Key,
                        psGroup.Sum(o => o.Amount),
                        [.. psGroup
                            .SelectMany(o => o.Splits)
                            .GroupBy(s => s.PersonId)
                            .Select(g => new PersonAmountDto(g.Key, g.Sum(s => s.Value)))
                            .OrderBy(c => c.PersonId)]))
                    .OrderBy(ps => ps.PaymentSourceId)]);
        }

        /// <summary>
        /// A payer group never owns income payments directly — a group's income is the sum of
        /// its members' income, wherever those income payments actually live. This mirrors
        /// <see cref="BuildDirectionTotals"/> but sums each member's split value rather than the
        /// occurrence's full amount, since an income payment split between people outside the
        /// group must not be counted in full against the group.
        /// </summary>
        private static DirectionTotalsDto BuildMemberIncomeTotals(
            IEnumerable<OccurrenceDto> incomingOccurrences,
            IReadOnlySet<Guid> memberIds)
        {
            var rows = incomingOccurrences
                .SelectMany(o => o.Splits
                    .Where(s => memberIds.Contains(s.PersonId))
                    .Select(s => (Occurrence: o, Split: s)))
                .ToArray();

            return new DirectionTotalsDto(
                rows.Sum(r => r.Split.Value),
                [.. rows
                    .GroupBy(r => r.Split.PersonId)
                    .Select(g => new PersonAmountDto(g.Key, g.Sum(r => r.Split.Value)))
                    .OrderBy(c => c.PersonId)],
                [.. rows
                    .GroupBy(r => r.Occurrence.PaymentSourceId)
                    .Select(psGroup => new PaymentSourceBreakdownDto(
                        psGroup.Key,
                        psGroup.Sum(r => r.Split.Value),
                        [.. psGroup
                            .GroupBy(r => r.Split.PersonId)
                            .Select(g => new PersonAmountDto(g.Key, g.Sum(r => r.Split.Value)))
                            .OrderBy(c => c.PersonId)]))
                    .OrderBy(ps => ps.PaymentSourceId)]);
        }

        private static ICollection<PayeeAmountDto> BuildPayeeTotals(IEnumerable<OccurrenceDto> occurrences) =>
            [.. occurrences
                .GroupBy(o => o.PayeeId)
                .Select(g => new PayeeAmountDto(g.Key, g.Sum(o => o.Amount)))
                .OrderBy(p => p.PayeeId)];

        /// <summary>
        /// Net is Incoming minus Outgoing — the "available cash" figure. A person who only
        /// appears on one side (e.g. income but no bills) still gets a Net row, with the missing
        /// side treated as zero.
        /// </summary>
        private static NetTotalsDto BuildNetTotals(DirectionTotalsDto incoming, DirectionTotalsDto outgoing)
        {
            var incomingByPerson = incoming.PersonTotals.ToDictionary(c => c.PersonId, c => c.Amount);
            var outgoingByPerson = outgoing.PersonTotals.ToDictionary(c => c.PersonId, c => c.Amount);
            var personIds = incomingByPerson.Keys.Union(outgoingByPerson.Keys);

            var personTotals = personIds
                .Select(id => new PersonAmountDto(
                    id,
                    incomingByPerson.GetValueOrDefault(id) - outgoingByPerson.GetValueOrDefault(id)))
                .OrderBy(c => c.PersonId)
                .ToArray();

            return new NetTotalsDto(
                incoming.TotalAmount - outgoing.TotalAmount,
                personTotals);
        }
    }

    public record Response(
        ICollection<OccurrenceDto> Occurrences,
        ICollection<GroupSummaryDto> Summary,
        ICollection<PersonCommitmentDto> People)
    {
        public record OccurrenceDto(
            Guid PaymentId,
            Guid PaymentSourceId,
            Guid PayeeId,
            decimal Amount,
            string Currency,
            PaymentFrequency Frequency,
            PaymentDirection Direction,
            DateOnly OccurrenceDate,
            DateOnly StartDate,
            DateOnly? EndDate,
            string? Description,
            Guid? PayerGroupId,
            ICollection<OccurrenceDto.SplitDto> Splits)
        {
            public record SplitDto(Guid PersonId, decimal Percentage, decimal Value);
        }

        /// <summary>One entry per real payer group the user has payments in. Ungrouped payments
        /// are not a group — their shares appear in <see cref="PersonCommitmentDto"/>.</summary>
        public record GroupSummaryDto(
            Guid PayerGroupId,
            ICollection<CurrencySummaryDto> Currencies);

        public record CurrencySummaryDto(
            string Currency,
            DirectionTotalsDto Outgoing,
            DirectionTotalsDto Incoming,
            NetTotalsDto Net,
            ICollection<PayeeAmountDto> OutgoingByPayee);

        public record DirectionTotalsDto(
            decimal TotalAmount,
            ICollection<PersonAmountDto> PersonTotals,
            ICollection<PaymentSourceBreakdownDto> ByPaymentSource);

        public record NetTotalsDto(
            decimal TotalAmount,
            ICollection<PersonAmountDto> PersonTotals);

        public record PersonAmountDto(Guid PersonId, decimal Amount);

        public record PayeeAmountDto(Guid PayeeId, decimal Amount);

        public record PaymentSourceBreakdownDto(
            Guid PaymentSourceId,
            decimal TotalAmount,
            ICollection<PersonAmountDto> PersonTotals);

        /// <summary>A person's income, committed outgoing share, and headroom across every
        /// payer group they belong to plus any personal (ungrouped) payments, for one
        /// currency. <see cref="IsOverCommitted"/> is true when their committed share of
        /// outgoings exceeds their income for the period.</summary>
        public record PersonCommitmentDto(
            Guid PersonId,
            string Currency,
            decimal Income,
            decimal Committed,
            decimal Remaining,
            bool IsOverCommitted);
    }
}
