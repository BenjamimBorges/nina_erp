using NinaERP.Domain.Enums;
using Xunit;

namespace NinaERP.Tests;

/// <summary>
/// Alguns valores são persistidos no PostgreSQL e utilizados pelas views oficiais.
/// Alterá-los exige migration explícita e atualização das views.
/// </summary>
public sealed class PersistedEnumContractTests
{
    [Fact]
    public void SaleStatus_Values_Used_By_Reporting_Must_Remain_Stable()
    {
        Assert.Equal(3, (int)SaleStatus.Completed);
        Assert.Equal(5, (int)SaleStatus.Cancelled);
        Assert.Equal(6, (int)SaleStatus.ReconciliationRequired);
    }

    [Fact]
    public void PaymentStatus_Values_Used_By_Reporting_Must_Remain_Stable()
    {
        Assert.Equal(2, (int)PaymentTransactionStatus.Authorized);
        Assert.Equal(3, (int)PaymentTransactionStatus.Captured);
        Assert.Equal(6, (int)PaymentTransactionStatus.Refunded);
        Assert.Equal(7, (int)PaymentTransactionStatus.PartiallyRefunded);
        Assert.Equal(8, (int)PaymentTransactionStatus.ReconciliationRequired);
    }

    [Fact]
    public void CashMovement_Values_Used_By_Reporting_Must_Remain_Stable()
    {
        Assert.Equal(0, (int)CashMovementType.Opening);
        Assert.Equal(2, (int)CashMovementType.Withdrawal);
        Assert.Equal(3, (int)CashMovementType.SaleReceipt);
        Assert.Equal(4, (int)CashMovementType.Refund);
    }

    [Fact]
    public void Authorized_And_Captured_Must_Never_Be_Equivalent()
    {
        Assert.NotEqual(PaymentTransactionStatus.Authorized, PaymentTransactionStatus.Captured);
    }
}
