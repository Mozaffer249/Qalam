using Qalam.Service.Implementations;
using Qalam.Service.Payments;

namespace Qalam.Service.Tests;

public class MoyasarRefundAndWebhookAuthTests
{
    [Fact]
    public async Task MockGateway_Refund_ReturnsMockId()
    {
        var gateway = new MockPaymentGateway();
        var dto = await gateway.RefundAsync("MOCK-pay", 1500);
        Assert.StartsWith("MOCK-REF-", dto.Id);
        Assert.Equal(1500, dto.AmountHalalas);
    }

    [Fact]
    public async Task MockGateway_Fetch_ReturnsPaid()
    {
        var gateway = new MockPaymentGateway();
        var paid = await gateway.FetchAsync("any-id");
        Assert.NotNull(paid);
        Assert.Equal("paid", paid!.Status);
        Assert.True(MoyasarStatusMapper.IsPaid(paid.Status));
    }

    [Fact]
    public void Moyasar_ConstantTimeEquals_RejectsMismatch()
    {
        Assert.False(MoyasarPaymentGateway.ConstantTimeEquals("a", "b"));
        Assert.True(MoyasarPaymentGateway.ConstantTimeEquals("same", "same"));
    }
}
