using System.Text;
using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    [HttpGet("purchases")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PurchaseReportDto>>>> GetPurchases([FromQuery] DateRangeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<PurchaseReportDto>>.Ok(await service.GetPurchaseReportAsync(request, cancellationToken)));

    [HttpGet("purchases/csv")]
    public async Task<IActionResult> ExportPurchasesCsv([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var csv = service.ExportPurchasesCsv(await service.GetPurchaseReportAsync(request, cancellationToken));
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "compras.csv");
    }

    [HttpGet("sales")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SaleReportDto>>>> GetSales([FromQuery] DateRangeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<SaleReportDto>>.Ok(await service.GetSaleReportAsync(request, cancellationToken)));

    [HttpGet("sales/csv")]
    public async Task<IActionResult> ExportSalesCsv([FromQuery] DateRangeRequest request, CancellationToken cancellationToken)
    {
        var csv = service.ExportSalesCsv(await service.GetSaleReportAsync(request, cancellationToken));
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "ventas.csv");
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InventoryReportDto>>>> GetInventory(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<InventoryReportDto>>.Ok(await service.GetInventoryReportAsync(cancellationToken)));

    [HttpGet("inventory/csv")]
    public async Task<IActionResult> ExportInventoryCsv(CancellationToken cancellationToken)
    {
        var csv = service.ExportInventoryCsv(await service.GetInventoryReportAsync(cancellationToken));
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "inventario.csv");
    }

    [HttpGet("product-costs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductCostReportDto>>>> GetProductCosts(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<ProductCostReportDto>>.Ok(await service.GetProductCostReportAsync(cancellationToken)));

    [HttpGet("product-profitability")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductProfitabilityReportDto>>>> GetProductProfitability([FromQuery] DateRangeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<ProductProfitabilityReportDto>>.Ok(await service.GetProductProfitabilityReportAsync(request, cancellationToken)));
}
