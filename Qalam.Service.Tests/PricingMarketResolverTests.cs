using Microsoft.AspNetCore.Identity;
using Moq;
using Qalam.Data.AppMetaData;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class PricingMarketResolverTests
{
    private static PricingMarket CreateMarket(string code, string currency, bool isDefault = false) =>
        new()
        {
            Code = code,
            Currency = currency,
            NameEn = code,
            NameAr = code,
            IsActive = true,
            IsDefault = isDefault
        };

    private static (PricingMarketResolver Resolver, Mock<UserManager<User>> UserManager, Mock<IPricingMarketRepository> MarketRepo)
        CreateSut(User? user = null, PricingMarket? defaultMarket = null)
    {
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(user);

        var marketRepo = new Mock<IPricingMarketRepository>();
        marketRepo
            .Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultMarket ?? CreateMarket(PricingMarketDefaults.DefaultMarketCode, "SAR", isDefault: true));
        marketRepo
            .Setup(r => r.GetByCodeAsync(PricingMarketDefaults.DefaultMarketCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultMarket ?? CreateMarket(PricingMarketDefaults.DefaultMarketCode, "SAR", isDefault: true));

        var resolver = new PricingMarketResolver(userManager.Object, marketRepo.Object);
        return (resolver, userManager, marketRepo);
    }

    [Fact]
    public async Task ResolveForUserAsync_UsesPreferredMarket_WhenSet()
    {
        var user = new User { Id = 1, PreferredMarketCode = "eg" };
        var (resolver, _, marketRepo) = CreateSut(user);
        marketRepo.Setup(r => r.GetByCodeAsync("eg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket("eg", "EGP"));

        var resolved = await resolver.ResolveForUserAsync(1);

        Assert.Equal("eg", resolved.MarketCode);
        Assert.Equal("EGP", resolved.Currency);
        Assert.Equal(PricingMarketResolutionSource.Preferred, resolved.Source);
    }

    [Fact]
    public async Task ResolveForUserAsync_UsesNationality_WhenNoPreference()
    {
        var user = new User { Id = 1, Nationality = "AE" };
        var (resolver, _, marketRepo) = CreateSut(user);
        marketRepo.Setup(r => r.GetByCodeAsync("ae", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket("ae", "AED"));

        var resolved = await resolver.ResolveForUserAsync(1);

        Assert.Equal("ae", resolved.MarketCode);
        Assert.Equal(PricingMarketResolutionSource.Nationality, resolved.Source);
    }

    [Fact]
    public async Task ResolveForUserAsync_UsesPhoneDialCode_WhenNationalityUnmapped()
    {
        var user = new User { Id = 1, PhoneNumber = "+966501234567" };
        var (resolver, _, marketRepo) = CreateSut(user);
        marketRepo.Setup(r => r.GetByCodeAsync("sa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMarket("sa", "SAR", isDefault: true));

        var resolved = await resolver.ResolveForUserAsync(1);

        Assert.Equal("sa", resolved.MarketCode);
        Assert.Equal(PricingMarketResolutionSource.Phone, resolved.Source);
    }

    [Fact]
    public async Task ResolveForUserAsync_FallsBackToDefault()
    {
        var user = new User { Id = 1, Nationality = "US" };
        var (resolver, _, _) = CreateSut(user);

        var resolved = await resolver.ResolveForUserAsync(1);

        Assert.Equal(PricingMarketDefaults.DefaultMarketCode, resolved.MarketCode);
        Assert.Equal(PricingMarketResolutionSource.Default, resolved.Source);
    }
}
