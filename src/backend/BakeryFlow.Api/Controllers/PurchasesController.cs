using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class PurchasesController(IPurchaseService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseListItemDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<PurchaseListItemDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PurchaseDetailDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PurchaseDetailDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseDetailDto>>> Create([FromBody] SavePurchaseRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PurchaseDetailDto>.Ok(await service.CreateAsync(request, cancellationToken), "Compra creada en borrador."));

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<PurchaseDetailDto>>> Confirm(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PurchaseDetailDto>.Ok(await service.ConfirmAsync(id, cancellationToken), "Compra confirmada."));
}
