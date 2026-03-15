using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<CategoryDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OptionDto>>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<OptionDto>>.Ok(await service.GetOptionsAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CategoryDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] SaveCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CategoryDto>.Ok(await service.CreateAsync(request, cancellationToken), "Categoría creada."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(Guid id, [FromBody] SaveCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<CategoryDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Categoría actualizada."));

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await service.ToggleStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Estado actualizado."));
    }
}
