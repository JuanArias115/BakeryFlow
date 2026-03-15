using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Ingredients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class IngredientsController(IIngredientService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<IngredientDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<IngredientDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("options")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<OptionDto>>>> GetOptions(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyCollection<OptionDto>>.Ok(await service.GetOptionsAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IngredientDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IngredientDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<IngredientDto>>> Create([FromBody] SaveIngredientRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IngredientDto>.Ok(await service.CreateAsync(request, cancellationToken), "Ingrediente creado."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<IngredientDto>>> Update(Guid id, [FromBody] SaveIngredientRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<IngredientDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Ingrediente actualizado."));

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await service.ToggleStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Estado actualizado."));
    }
}
