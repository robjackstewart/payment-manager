using System;
using PaymentManager.Domain.Entities;

namespace PaymentManager.Application.Common;

public interface IReadOnlyPaymentManagerContext
{
    public IQueryable<User> Users { get; }
    public IQueryable<PaymentSource> PaymentSources { get; }
    public IQueryable<Payee> Payees { get; }
    public IQueryable<Payment> Payments { get; }
    public IQueryable<Person> People { get; }
    public IQueryable<PayerGroupMember> PayerGroupMembers { get; }
    public IQueryable<PayerGroup> PayerGroups { get; }
    public IQueryable<PaymentSplit> PaymentSplits { get; }
    public IQueryable<EffectivePaymentSplit> EffectivePaymentSplits { get; }
    public IQueryable<EffectivePaymentValue> EffectivePaymentValues { get; }
}
