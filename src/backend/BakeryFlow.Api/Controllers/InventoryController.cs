using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController(IInventoryService service) : ControllerBase
{
    [HttpGet("stocks")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryStockDto>>>> GetStocks([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<InventoryStockDto>>.Ok(await service.GetStocksAsync(request, cancellationToken)));

    [HttpGet("movements")]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryMovementDto>>>> GetMovements([FromQuery] InventoryMovementFilterRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<InventoryMovementDto>>.Ok(await service.GetMovementsAsync(request, cancellationToken)));

    [HttpPost("adjustments")]
    public async Task<ActionResult<ApiResponse<InventoryAdjustmentResultDto>>> CreateAdjustment([FromBody] CreateInventoryAdjustmentRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<InventoryAdjustmentResultDto>.Ok(await service.CreateAdjustmentAsync(request, cancellationToken), "Ajuste registrado."));
}
