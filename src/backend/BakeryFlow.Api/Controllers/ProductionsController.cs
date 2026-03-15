using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Productions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProductionsController(IProductionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductionListItemDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<ProductionListItemDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductionDetailDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProductionDetailDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<ProductionPreviewDto>>> Preview([FromBody] CreateProductionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProductionPreviewDto>.Ok(await service.PreviewAsync(request, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductionDetailDto>>> Create([FromBody] CreateProductionRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProductionDetailDto>.Ok(await service.CreateAsync(request, cancellationToken), "Producción registrada."));
}
