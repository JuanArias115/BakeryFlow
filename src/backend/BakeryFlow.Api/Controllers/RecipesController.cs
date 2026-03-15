using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Features.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BakeryFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class RecipesController(IRecipeService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RecipeListItemDto>>>> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<PagedResult<RecipeListItemDto>>.Ok(await service.GetPagedAsync(request, cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RecipeDetailDto>>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RecipeDetailDto>.Ok(await service.GetByIdAsync(id, cancellationToken)));

    [HttpGet("costing/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<RecipeCostingDto>>> GetCosting(Guid productId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RecipeCostingDto>.Ok(await service.GetCostingByProductAsync(productId, cancellationToken)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RecipeDetailDto>>> Create([FromBody] SaveRecipeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RecipeDetailDto>.Ok(await service.CreateAsync(request, cancellationToken), "Receta creada."));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RecipeDetailDto>>> Update(Guid id, [FromBody] SaveRecipeRequest request, CancellationToken cancellationToken) =>
        Ok(ApiResponse<RecipeDetailDto>.Ok(await service.UpdateAsync(id, request, cancellationToken), "Receta actualizada."));
}
