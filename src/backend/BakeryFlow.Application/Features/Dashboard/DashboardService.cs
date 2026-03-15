using BakeryFlow.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

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

public sealed class DashboardService(IBakeryFlowDbContext dbContext) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var salesToday = await dbContext.Sales
            .AsNoTracking()
            .Where(x => x.Date.Date == today)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var salesMonth = await dbContext.Sales
            .AsNoTracking()
            .Where(x => x.Date >= monthStart)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var purchasesMonth = await dbContext.Purchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= monthStart)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var productsCount = await dbContext.Products.CountAsync(cancellationToken);
        var ingredientsCount = await dbContext.Ingredients.CountAsync(cancellationToken);

        var topProfitableProducts = await dbContext.SaleDetails
            .AsNoTracking()
            .Include(x => x.Product)
            .GroupBy(x => new { x.ProductId, x.Product!.Name })
            .Select(group => new DashboardTopItemDto(
                group.Key.ProductId,
                group.Key.Name,
                group.Sum(x => x.Profit),
                group.Sum(x => x.Quantity)))
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topSellingProducts = await dbContext.SaleDetails
            .AsNoTracking()
            .Include(x => x.Product)
            .GroupBy(x => new { x.ProductId, x.Product!.Name })
            .Select(group => new DashboardTopItemDto(
                group.Key.ProductId,
                group.Key.Name,
                group.Sum(x => x.Quantity),
                group.Sum(x => x.Subtotal)))
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

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
            var day = today.AddDays(-index);
            var value = await dbContext.Sales
                .AsNoTracking()
                .Where(x => x.Date.Date == day)
                .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

            dailySalesChart.Add(new DashboardChartPointDto(day.ToString("dd/MM"), value, null));
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
