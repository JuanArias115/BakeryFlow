using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class SalesController(ISaleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SaleListItemDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<SaleListItemDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SaleDetailDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SaleDetailDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SaleDetailDto>>> Create([FromBody] CreateSaleRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SaleDetailDto>.Ok(await service.CreateAsync(request, cancellationToken), "Venta registrada."));
}
