using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BakeryFlow.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardTopItemDto(Guid Id, string Name, decimal Value, decimal SecondaryValue);

public sealed record DashboardChartPointDto(string Label, decimal Value, decimal? SecondaryValue);

public sealed record DashboardLowStockDto(Guid IngredientId, string IngredientName, decimal StockCurrent, decimal StockMinimum);

public sealed record DashboardDto(
    decimal SalesToday,
    decimal SalesMonth,
    decimal PurchasesMonth,
    int ProductsCount,
    int IngredientsCount,
    IReadOnlyCollection<DashboardTopItemDto> TopProfitableProducts,
    IReadOnlyCollection<DashboardTopItemDto> TopSellingProducts,
    IReadOnlyCollection<DashboardLowStockDto> LowStockIngredients,
    IReadOnlyCollection<DashboardChartPointDto> DailySalesChart,
    IReadOnlyCollection<DashboardChartPointDto> MonthlyFlowChart);

public sealed class DashboardService(
    IBakeryFlowDbContext dbContext,
    ILogger<DashboardService> logger) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var todayStart = UtcDateTime.StartOfDay(nowUtc);
        var tomorrowStart = todayStart.AddDays(1);
        var monthStart = UtcDateTime.StartOfMonth(nowUtc);
        var nextMonthStart = monthStart.AddMonths(1);

        logger.LogDebug(
            "Building dashboard with UTC ranges. TodayStart={TodayStart}, MonthStart={MonthStart}",
            todayStart,
            monthStart);

        var salesToday = await dbContext.Sales
            .AsNoTracking()
            .Where(x => x.Date >= todayStart && x.Date < tomorrowStart)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var salesMonth = await dbContext.Sales
            .AsNoTracking()
            .Where(x => x.Date >= monthStart && x.Date < nextMonthStart)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var purchasesMonth = await dbContext.Purchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= monthStart && x.PurchaseDate < nextMonthStart)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var productsCount = await dbContext.Products.CountAsync(cancellationToken);
        var ingredientsCount = await dbContext.Ingredients.CountAsync(cancellationToken);

        var topProfitableProducts = (await dbContext.SaleDetails
                .AsNoTracking()
                .GroupBy(x => new { x.ProductId, ProductName = x.Product!.Name })
                .Select(group => new
                {
                    group.Key.ProductId,
                    group.Key.ProductName,
                    Profit = group.Sum(x => x.Profit),
                    Quantity = group.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Profit)
                .ThenBy(x => x.ProductName)
                .Take(5)
                .ToListAsync(cancellationToken))
            .Select(x => new DashboardTopItemDto(x.ProductId, x.ProductName, x.Profit, x.Quantity))
            .ToList();

        var topSellingProducts = (await dbContext.SaleDetails
                .AsNoTracking()
                .GroupBy(x => new { x.ProductId, ProductName = x.Product!.Name })
                .Select(group => new
                {
                    group.Key.ProductId,
                    group.Key.ProductName,
                    Quantity = group.Sum(x => x.Quantity),
                    Revenue = group.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenBy(x => x.ProductName)
                .Take(5)
                .ToListAsync(cancellationToken))
            .Select(x => new DashboardTopItemDto(x.ProductId, x.ProductName, x.Quantity, x.Revenue))
            .ToList();

        var lowStockIngredients = await dbContext.Ingredients
            .AsNoTracking()
            .Where(x => x.StockCurrent <= x.StockMinimum)
            .OrderBy(x => x.StockCurrent)
            .Take(8)
            .Select(x => new DashboardLowStockDto(x.Id, x.Name, x.StockCurrent, x.StockMinimum))
            .ToListAsync(cancellationToken);

        var dailySalesChart = new List<DashboardChartPointDto>();
        for (var index = 6; index >= 0; index--)
        {
            var dayStart = todayStart.AddDays(-index);
            var dayEnd = dayStart.AddDays(1);
            var value = await dbContext.Sales
                .AsNoTracking()
                .Where(x => x.Date >= dayStart && x.Date < dayEnd)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

            dailySalesChart.Add(new DashboardChartPointDto(dayStart.ToString("dd/MM"), value, null));
        }

        var monthlyFlowChart = new List<DashboardChartPointDto>();
        for (var index = 5; index >= 0; index--)
        {
            var month = monthStart.AddMonths(-index);
            var next = month.AddMonths(1);

            var monthSales = await dbContext.Sales
                .AsNoTracking()
                .Where(x => x.Date >= month && x.Date < next)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

            var monthPurchases = await dbContext.Purchases
                .AsNoTracking()
                .Where(x => x.PurchaseDate >= month && x.PurchaseDate < next)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

            monthlyFlowChart.Add(new DashboardChartPointDto(month.ToString("MMM"), monthSales, monthPurchases));
        }

        return new DashboardDto(
            salesToday,
            salesMonth,
            purchasesMonth,
            productsCount,
            ingredientsCount,
            topProfitableProducts,
            topSellingProducts,
            lowStockIngredients,
            dailySalesChart,
            monthlyFlowChart);
    }
}
