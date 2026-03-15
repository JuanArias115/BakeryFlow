using System.Text;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Reports;

public interface IReportService
{
    Task<IReadOnlyCollection<PurchaseReportDto>> GetPurchaseReportAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SaleReportDto>> GetSaleReportAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<InventoryReportDto>> GetInventoryReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductCostReportDto>> GetProductCostReportAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductProfitabilityReportDto>> GetProductProfitabilityReportAsync(DateRangeRequest request, CancellationToken cancellationToken = default);
    string ExportPurchasesCsv(IEnumerable<PurchaseReportDto> items);
    string ExportSalesCsv(IEnumerable<SaleReportDto> items);
    string ExportInventoryCsv(IEnumerable<InventoryReportDto> items);
}

public sealed record PurchaseReportDto(
    Guid PurchaseId,
    DateTime PurchaseDate,
    string SupplierName,
    string? InvoiceNumber,
    string Status,
    decimal Total);

public sealed record SaleReportDto(
    Guid SaleId,
    DateTime Date,
    string? CustomerName,
    string PaymentMethod,
    decimal Total,
    decimal Profit);

public sealed record InventoryReportDto(
    Guid IngredientId,
    string IngredientName,
    string UnitName,
    decimal StockCurrent,
    decimal StockMinimum,
    decimal AverageCost,
    decimal InventoryValue);

public sealed record ProductCostReportDto(
    Guid ProductId,
    string ProductName,
    decimal SalePrice,
    decimal RecipeCost,
    decimal UnitCost,
    decimal EstimatedProfit);

public sealed record ProductProfitabilityReportDto(
    Guid ProductId,
    string ProductName,
    decimal QuantitySold,
    decimal Revenue,
    decimal Profit);

public sealed class ReportService(IBakeryFlowDbContext dbContext) : IReportService
{
    public async Task<IReadOnlyCollection<PurchaseReportDto>> GetPurchaseReportAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var fromUtc = UtcDateTime.EnsureUtc(request.From);
        var toUtc = UtcDateTime.EnsureUtc(request.To);

        return await dbContext.Purchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x =>
                (!fromUtc.HasValue || x.PurchaseDate >= fromUtc.Value) &&
                (!toUtc.HasValue || x.PurchaseDate <= toUtc.Value))
            .OrderByDescending(x => x.PurchaseDate)
            .Select(x => new PurchaseReportDto(
                x.Id,
                x.PurchaseDate,
                x.Supplier!.Name,
                x.InvoiceNumber,
                x.Status.ToString(),
                x.Total))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SaleReportDto>> GetSaleReportAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var fromUtc = UtcDateTime.EnsureUtc(request.From);
        var toUtc = UtcDateTime.EnsureUtc(request.To);

        return await dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Details)
            .Where(x =>
                (!fromUtc.HasValue || x.Date >= fromUtc.Value) &&
                (!toUtc.HasValue || x.Date <= toUtc.Value))
            .OrderByDescending(x => x.Date)
            .Select(x => new SaleReportDto(
                x.Id,
                x.Date,
                x.Customer != null ? x.Customer.Name : null,
                x.PaymentMethod.ToString(),
                x.Total,
                x.Details.Sum(d => d.Profit)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<InventoryReportDto>> GetInventoryReportAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Ingredients
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .OrderBy(x => x.Name)
            .Select(x => new InventoryReportDto(
                x.Id,
                x.Name,
                x.UnitOfMeasure!.Abbreviation,
                x.StockCurrent,
                x.StockMinimum,
                x.AverageCost,
                x.StockCurrent * x.AverageCost))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProductCostReportDto>> GetProductCostReportAsync(CancellationToken cancellationToken = default)
    {
        var recipes = await dbContext.Recipes
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Details)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Product!.Name)
            .ToListAsync(cancellationToken);

        return recipes.Select(x =>
        {
            var recipeCost = x.Details.Sum(d => d.CalculatedTotalCost) + x.PackagingCost;
            var unitCost = recipeCost / x.Yield;

            return new ProductCostReportDto(
                x.ProductId,
                x.Product!.Name,
                x.Product.SalePrice,
                recipeCost,
                unitCost,
                x.Product.SalePrice - unitCost);
        }).ToList();
    }

    public async Task<IReadOnlyCollection<ProductProfitabilityReportDto>> GetProductProfitabilityReportAsync(DateRangeRequest request, CancellationToken cancellationToken = default)
    {
        var fromUtc = UtcDateTime.EnsureUtc(request.From);
        var toUtc = UtcDateTime.EnsureUtc(request.To);

        return await dbContext.SaleDetails
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Sale)
            .Where(x =>
                (!fromUtc.HasValue || x.Sale!.Date >= fromUtc.Value) &&
                (!toUtc.HasValue || x.Sale!.Date <= toUtc.Value))
            .GroupBy(x => new { x.ProductId, x.Product!.Name })
            .OrderByDescending(x => x.Sum(y => y.Profit))
            .Select(group => new ProductProfitabilityReportDto(
                group.Key.ProductId,
                group.Key.Name,
                group.Sum(x => x.Quantity),
                group.Sum(x => x.Subtotal),
                group.Sum(x => x.Profit)))
            .ToListAsync(cancellationToken);
    }

    public string ExportPurchasesCsv(IEnumerable<PurchaseReportDto> items) =>
        BuildCsv(
            "Fecha,Proveedor,Factura,Estado,Total",
            items.Select(x => $"{x.PurchaseDate:yyyy-MM-dd},{Escape(x.SupplierName)},{Escape(x.InvoiceNumber)},{x.Status},{x.Total:F2}"));

    public string ExportSalesCsv(IEnumerable<SaleReportDto> items) =>
        BuildCsv(
            "Fecha,Cliente,MetodoPago,Total,Utilidad",
            items.Select(x => $"{x.Date:yyyy-MM-dd},{Escape(x.CustomerName)},{x.PaymentMethod},{x.Total:F2},{x.Profit:F2}"));

    public string ExportInventoryCsv(IEnumerable<InventoryReportDto> items) =>
        BuildCsv(
            "Ingrediente,Unidad,StockActual,StockMinimo,CostoPromedio,ValorInventario",
            items.Select(x => $"{Escape(x.IngredientName)},{x.UnitName},{x.StockCurrent:F2},{x.StockMinimum:F2},{x.AverageCost:F4},{x.InventoryValue:F2}"));

    private static string BuildCsv(string header, IEnumerable<string> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(header);

        foreach (var row in rows)
        {
            builder.AppendLine(row);
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
