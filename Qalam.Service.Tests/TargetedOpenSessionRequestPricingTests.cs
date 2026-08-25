using Moq;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Entity.Pricing;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using Qalam.Service.Implementations;
using Qalam.Service.Models.Pricing;

namespace Qalam.Service.Tests;

public class TargetedOpenSessionRequestPricingTests
{
    private static (
        TargetedOpenSessionRequestPricingService Sut,
        Mock<IPricingMarketResolver> MarketResolver,
        Mock<IPricingSnapshotWriter> SnapshotWriter,
        Mock<IOpenSessionRequestRepository> RequestRepo)
        CreateSut()
    {
        var marketResolver = new Mock<IPricingMarketResolver>();
        marketResolver
            .Setup(r => r.ResolveForUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPricingMarket
            {
                MarketCode = "sa",
                Currency = "SAR",
                NameEn = "Saudi Arabia",
                NameAr = "السعودية",
                Source = PricingMarketResolutionSource.Default
            });

        var snapshotWriter = new Mock<IPricingSnapshotWriter>();
        var requestRepo = new Mock<IOpenSessionRequestRepository>();
        requestRepo.Setup(r => r.UpdateAsync(It.IsAny<OpenSessionRequest>())).Returns(Task.CompletedTask);
        requestRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var sut = new TargetedOpenSessionRequestPricingService(
            marketResolver.Object,
            snapshotWriter.Object,
            requestRepo.Object);

        return (sut, marketResolver, snapshotWriter, requestRepo);
    }

    [Fact]
    public async Task FreezeIfNeededAsync_TargetedWithoutSnapshot_CreatesAndAttachesSnapshot()
    {
        var (sut, _, snapshotWriter, requestRepo) = CreateSut();
        var request = new OpenSessionRequest
        {
            Id = 10,
            DomainId = 5,
            TargetedTeacherId = 42,
            GroupType = null,
            Sessions =
            [
                new OpenSessionRequestSession { DurationMinutes = 60 },
                new OpenSessionRequestSession { DurationMinutes = 60 },
            ]
        };

        snapshotWriter
            .Setup(w => w.CreateAndSaveAsync(
                It.Is<CreatePricingSnapshotRequest>(r =>
                    r.Context == PricingSnapshotContext.OpenSessionRequest
                    && r.ContextEntityId == 10
                    && r.TeacherId == 42
                    && r.TotalMinutes == 120
                    && r.SessionTypeCode == "individual"
                    && r.MarketCode == "sa"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingSnapshot
            {
                Id = 99,
                Context = PricingSnapshotContext.OpenSessionRequest,
                ContextEntityId = 10,
                TeacherId = 42,
                TotalPrice = 110m,
                TotalMinutes = 120,
                Currency = "SAR",
                MarketCode = "sa",
                SessionTypeCode = "individual"
            });

        await sut.FreezeIfNeededAsync(request, marketUserId: 7);

        Assert.Equal(99, request.PricingSnapshotId);
        requestRepo.Verify(r => r.UpdateAsync(request), Times.Once);
        requestRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task FreezeIfNeededAsync_Broadcast_DoesNotCreateSnapshot()
    {
        var (sut, _, snapshotWriter, requestRepo) = CreateSut();
        var request = new OpenSessionRequest
        {
            Id = 11,
            DomainId = 5,
            TargetedTeacherId = null,
            Sessions = [new OpenSessionRequestSession { DurationMinutes = 60 }]
        };

        await sut.FreezeIfNeededAsync(request, marketUserId: 7);

        Assert.Null(request.PricingSnapshotId);
        snapshotWriter.Verify(
            w => w.CreateAndSaveAsync(It.IsAny<CreatePricingSnapshotRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        requestRepo.Verify(r => r.UpdateAsync(It.IsAny<OpenSessionRequest>()), Times.Never);
    }

    [Fact]
    public async Task FreezeIfNeededAsync_AlreadyFrozen_DoesNotReEstimate()
    {
        var (sut, _, snapshotWriter, _) = CreateSut();
        var request = new OpenSessionRequest
        {
            Id = 12,
            DomainId = 5,
            TargetedTeacherId = 42,
            PricingSnapshotId = 77,
            Sessions = [new OpenSessionRequestSession { DurationMinutes = 60 }]
        };

        await sut.FreezeIfNeededAsync(request, marketUserId: 7);

        Assert.Equal(77, request.PricingSnapshotId);
        snapshotWriter.Verify(
            w => w.CreateAndSaveAsync(It.IsAny<CreatePricingSnapshotRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ToEstimateDto_MapsFrozenTotalPrice()
    {
        var (sut, _, _, _) = CreateSut();
        var snapshot = new PricingSnapshot
        {
            PricePerHour = 55m,
            Currency = "SAR",
            MarketCode = "sa",
            TotalMinutes = 120,
            TotalPrice = 110m,
            TeacherSharePct = 70m,
            TeacherEarnings = 77m,
            PlatformShare = 33m,
            EarningsPricePerHour = 50m,
            ReflectCustomPriceToStudent = true
        };

        var dto = sut.ToEstimateDto(snapshot);

        Assert.Equal(110m, dto.TotalPrice);
        Assert.Equal(55m, dto.PricePerHour);
        Assert.Equal("SAR", dto.Currency);
        Assert.Equal(120, dto.TotalMinutes);
        Assert.True(dto.ReflectCustomPriceToStudent);
    }

    [Fact]
    public async Task CloneForContextAsync_PreservesTotalPriceWithoutReEstimate()
    {
        var pricingEngine = new Mock<IPricingEngine>();
        var snapshotRepo = new Mock<IPricingSnapshotRepository>();
        PricingSnapshot? saved = null;
        snapshotRepo
            .Setup(r => r.AddAsync(It.IsAny<PricingSnapshot>()))
            .Callback<PricingSnapshot>(s =>
            {
                s.Id = 200;
                saved = s;
            })
            .ReturnsAsync((PricingSnapshot s) => s);
        snapshotRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var writer = new PricingSnapshotWriter(pricingEngine.Object, snapshotRepo.Object);
        var source = new PricingSnapshot
        {
            Id = 99,
            Context = PricingSnapshotContext.OpenSessionRequest,
            ContextEntityId = 10,
            DomainId = 5,
            SessionTypeCode = "individual",
            MarketCode = "sa",
            Currency = "SAR",
            PricePerHour = 55m,
            TotalMinutes = 120,
            TotalPrice = 110m,
            TeacherId = 42,
            TeacherSharePct = 70m,
            TeacherEarnings = 77m,
            PlatformShare = 33m
        };

        var clone = await writer.CloneForContextAsync(
            source,
            PricingSnapshotContext.OpenSessionOffer,
            contextEntityId: 55);

        Assert.Equal(110m, clone.TotalPrice);
        Assert.Equal(PricingSnapshotContext.OpenSessionOffer, clone.Context);
        Assert.Equal(55, clone.ContextEntityId);
        Assert.Equal(42, clone.TeacherId);
        Assert.NotNull(saved);
        pricingEngine.Verify(
            e => e.CreateSnapshotAsync(It.IsAny<CreatePricingSnapshotRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        pricingEngine.Verify(
            e => e.EstimateAsync(It.IsAny<PricingEstimateRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
