using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class AdminFinanceServiceTests
{
    private static ApplicationDBContext CreateDb()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDBContext(options, config);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsZeroTotals_WhenDatabaseEmpty()
    {
        await using var db = CreateDb();
        var service = new AdminFinanceService(new AdminFinanceReadRepository(db, new TeacherLedgerReadRepository(db)));

        var summary = await service.GetSummaryAsync(null, null);

        Assert.Equal(0m, summary.TotalCollected);
        Assert.Equal(0m, summary.TotalRefunds);
        Assert.Equal(0m, summary.PlatformNet);
        Assert.Equal("SAR", summary.Currency);
    }
}
