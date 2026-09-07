using Qalam.Data.Entity.Common.Enums;
using Qalam.Service.Payments;

namespace Qalam.Service.Tests;

public class MoyasarPaymentHelpersTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 100)]
    [InlineData(200.01, 20001)]
    [InlineData(12.345, 1235)]
    public void ToHalalas_RoundsAwayFromZero(decimal sar, int expected)
        => Assert.Equal(expected, MoyasarAmountConverter.ToHalalas(sar));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 1)]
    [InlineData(20001, 200.01)]
    public void FromHalalas_DividesBy100(int halalas, decimal expected)
        => Assert.Equal(expected, MoyasarAmountConverter.FromHalalas(halalas));

    [Theory]
    [InlineData("paid", PaymentStatus.Succeeded)]
    [InlineData("captured", PaymentStatus.Succeeded)]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("voided", PaymentStatus.Failed)]
    [InlineData("refunded", PaymentStatus.Refunded)]
    [InlineData("initiated", PaymentStatus.Pending)]
    [InlineData("authorized", PaymentStatus.Pending)]
    [InlineData(null, PaymentStatus.Pending)]
    public void MapStatus_CoversKnownValues(string? status, PaymentStatus expected)
        => Assert.Equal(expected, MoyasarStatusMapper.Map(status));

    [Theory]
    [InlineData("paid", true)]
    [InlineData("captured", true)]
    [InlineData("failed", false)]
    [InlineData("initiated", false)]
    public void IsPaid_OnlyPaidOrCaptured(string status, bool expected)
        => Assert.Equal(expected, MoyasarStatusMapper.IsPaid(status));
}
