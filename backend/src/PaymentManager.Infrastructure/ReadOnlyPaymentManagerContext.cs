using System;
using Microsoft.EntityFrameworkCore;
using PaymentManager.Application.Common;
using PaymentManager.Application.Common.Dispatch;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Infrastructure;

internal sealed class ReadOnlyPaymentManagerContext(IPaymentManagerContext context) : IReadOnlyPaymentManagerContext
{
    public IQueryable<User> Users => context.Users.AsNoTracking().AsQueryable();
    public IQueryable<PaymentSource> PaymentSources => context.PaymentSources.AsNoTracking().AsQueryable();
    public IQueryable<Payee> Payees => context.Payees.AsNoTracking().AsQueryable();
    public IQueryable<Payment> Payments => context.Payments.AsNoTracking().AsQueryable();
    public IQueryable<Person> People => context.People.AsNoTracking().AsQueryable();
    public IQueryable<PayerGroupMember> PayerGroupMembers => context.PayerGroupMembers.AsNoTracking().AsQueryable();
    public IQueryable<PayerGroup> PayerGroups => context.PayerGroups.AsNoTracking().AsQueryable();
    public IQueryable<PaymentSplit> PaymentSplits => context.PaymentSplits.AsNoTracking().AsQueryable();
    public IQueryable<EffectivePaymentSplit> EffectivePaymentSplits => context.EffectivePaymentSplits.AsNoTracking().AsQueryable();
    public IQueryable<EffectivePaymentValue> EffectivePaymentValues => context.EffectivePaymentValues.AsNoTracking().AsQueryable();
}
