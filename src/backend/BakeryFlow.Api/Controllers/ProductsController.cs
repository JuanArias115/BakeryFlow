using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<ProductDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OptionDto>>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<OptionDto>>.Ok(await service.GetOptionsAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProductDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] SaveProductRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProductDto>.Ok(await service.CreateAsync(request, cancellationToken), "Producto creado."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(Guid id, [FromBody] SaveProductRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<ProductDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Producto actualizado."));

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await service.ToggleStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Estado actualizado."));
    }
}
