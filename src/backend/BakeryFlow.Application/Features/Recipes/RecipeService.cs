using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Recipes;

public interface IRecipeService
{
    Task<PagedResult<RecipeListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<RecipeDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RecipeCostingDto> GetCostingByProductAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<RecipeDetailDto> CreateAsync(SaveRecipeRequest request, CancellationToken cancellationToken = default);
    Task<RecipeDetailDto> UpdateAsync(Guid id, SaveRecipeRequest request, CancellationToken cancellationToken = default);
}

public sealed record RecipeListItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Yield,
    string YieldUnit,
    decimal PackagingCost,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    decimal TotalRecipeCost,
    decimal UnitCost);

public sealed record RecipeDetailLineDto(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    Guid UnitOfMeasureId,
    string UnitName,
    decimal Quantity,
    decimal CalculatedUnitCost,
    decimal CalculatedTotalCost);

public sealed record RecipeDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal ProductSalePrice,
    decimal Yield,
    string YieldUnit,
    decimal PackagingCost,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    decimal IngredientsCost,
    decimal TotalRecipeCost,
    decimal UnitCost,
    decimal EstimatedGrossProfit,
    IReadOnlyCollection<RecipeDetailLineDto> Details);

public sealed record RecipeCostingDto(
    Guid ProductId,
    string ProductName,
    decimal SalePrice,
    decimal IngredientsCost,
    decimal PackagingCost,
    decimal TotalRecipeCost,
    decimal Yield,
    string YieldUnit,
    decimal UnitCost,
    decimal EstimatedGrossProfit);

public sealed record SaveRecipeLineRequest(Guid IngredientId, decimal Quantity, Guid UnitOfMeasureId);

public sealed record SaveRecipeRequest(
    Guid ProductId,
    decimal Yield,
    string YieldUnit,
    decimal PackagingCost,
    string? Notes,
    bool IsActive,
    IReadOnlyCollection<SaveRecipeLineRequest> Details);

public sealed class SaveRecipeRequestValidator : AbstractValidator<SaveRecipeRequest>
{
    public SaveRecipeRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Yield).GreaterThan(0);
        RuleFor(x => x.YieldUnit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PackagingCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(x => x.IngredientId).NotEmpty();
            detail.RuleFor(x => x.UnitOfMeasureId).NotEmpty();
            detail.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public sealed class RecipeService(IBakeryFlowDbContext dbContext) : IRecipeService
{
    public async Task<PagedResult<RecipeListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Recipes
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Details)
            .Where(x => string.IsNullOrWhiteSpace(term) || x.Product!.Name.ToLower().Contains(term))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecipeListItemDto(
                x.Id,
                x.ProductId,
                x.Product!.Name,
                x.Yield,
                x.YieldUnit,
                x.PackagingCost,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt,
                x.Details.Sum(d => d.CalculatedTotalCost) + x.PackagingCost,
                (x.Details.Sum(d => d.CalculatedTotalCost) + x.PackagingCost) / x.Yield));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<RecipeDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recipe = await dbContext.Recipes
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Details)
                .ThenInclude(x => x.Ingredient)
            .Include(x => x.Details)
                .ThenInclude(x => x.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return recipe is null ? throw new NotFoundException("Receta no encontrada.") : MapRecipe(recipe);
    }

    public async Task<RecipeCostingDto> GetCostingByProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var recipe = await dbContext.Recipes
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsActive, cancellationToken);

        if (recipe is null)
        {
            throw new NotFoundException("No existe una receta activa para el producto.");
        }

        var ingredientsCost = recipe.Details.Sum(x => x.CalculatedTotalCost);
        var total = ingredientsCost + recipe.PackagingCost;
        var unitCost = total / recipe.Yield;

        return new RecipeCostingDto(
            recipe.ProductId,
            recipe.Product!.Name,
            recipe.Product.SalePrice,
            ingredientsCost,
            recipe.PackagingCost,
            total,
            recipe.Yield,
            recipe.YieldUnit,
            unitCost,
            recipe.Product.SalePrice - unitCost);
    }

    public async Task<RecipeDetailDto> CreateAsync(SaveRecipeRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRecipeRequestAsync(request, cancellationToken);

        var recipe = new Recipe
        {
            ProductId = request.ProductId,
            Yield = request.Yield,
            YieldUnit = request.YieldUnit.Trim(),
            PackagingCost = request.PackagingCost,
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive
        };

        recipe.Details = await BuildRecipeDetailsAsync(request.Details, cancellationToken);
        await DeactivateOtherRecipesAsync(request.ProductId, request.IsActive, null, cancellationToken);

        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(recipe.Id, cancellationToken);
    }

    public async Task<RecipeDetailDto> UpdateAsync(Guid id, SaveRecipeRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateRecipeRequestAsync(request, cancellationToken);

        var recipe = await dbContext.Recipes
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Receta no encontrada.");

        recipe.ProductId = request.ProductId;
        recipe.Yield = request.Yield;
        recipe.YieldUnit = request.YieldUnit.Trim();
        recipe.PackagingCost = request.PackagingCost;
        recipe.Notes = request.Notes?.Trim();
        recipe.IsActive = request.IsActive;
        recipe.UpdatedAt = DateTime.UtcNow;

        dbContext.RecipeDetails.RemoveRange(recipe.Details);
        recipe.Details = await BuildRecipeDetailsAsync(request.Details, cancellationToken);

        await DeactivateOtherRecipesAsync(request.ProductId, request.IsActive, id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task ValidateRecipeRequestAsync(SaveRecipeRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new BusinessRuleException("El producto seleccionado no existe.");
        }
    }

    private async Task<List<RecipeDetail>> BuildRecipeDetailsAsync(
        IReadOnlyCollection<SaveRecipeLineRequest> details,
        CancellationToken cancellationToken)
    {
        var ingredientIds = details.Select(x => x.IngredientId).Distinct().ToList();
        var ingredients = await dbContext.Ingredients
            .Include(x => x.UnitOfMeasure)
            .Where(x => ingredientIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (ingredients.Count != ingredientIds.Count)
        {
            throw new BusinessRuleException("Uno o más ingredientes no existen.");
        }

        return details.Select(detail =>
        {
            var ingredient = ingredients.First(x => x.Id == detail.IngredientId);

            return new RecipeDetail
            {
                IngredientId = detail.IngredientId,
                Quantity = detail.Quantity,
                UnitOfMeasureId = detail.UnitOfMeasureId,
                CalculatedUnitCost = ingredient.AverageCost,
                CalculatedTotalCost = ingredient.AverageCost * detail.Quantity
            };
        }).ToList();
    }

    private async Task DeactivateOtherRecipesAsync(
        Guid productId,
        bool activateCurrent,
        Guid? currentRecipeId,
        CancellationToken cancellationToken)
    {
        if (!activateCurrent)
        {
            return;
        }

        var recipes = await dbContext.Recipes
            .Where(x => x.ProductId == productId && x.IsActive && x.Id != currentRecipeId)
            .ToListAsync(cancellationToken);

        foreach (var recipe in recipes)
        {
            recipe.IsActive = false;
            recipe.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static RecipeDetailDto MapRecipe(Recipe recipe)
    {
        var ingredientsCost = recipe.Details.Sum(x => x.CalculatedTotalCost);
        var total = ingredientsCost + recipe.PackagingCost;
        var unitCost = total / recipe.Yield;
        var salePrice = recipe.Product?.SalePrice ?? 0m;

        return new RecipeDetailDto(
            recipe.Id,
            recipe.ProductId,
            recipe.Product?.Name ?? string.Empty,
            salePrice,
            recipe.Yield,
            recipe.YieldUnit,
            recipe.PackagingCost,
            recipe.Notes,
            recipe.IsActive,
            recipe.CreatedAt,
            recipe.UpdatedAt,
            ingredientsCost,
            total,
            unitCost,
            salePrice - unitCost,
            recipe.Details
                .OrderBy(x => x.Ingredient!.Name)
                .Select(x => new RecipeDetailLineDto(
                    x.Id,
                    x.IngredientId,
                    x.Ingredient!.Name,
                    x.UnitOfMeasureId,
                    x.UnitOfMeasure!.Abbreviation,
                    x.Quantity,
                    x.CalculatedUnitCost,
                    x.CalculatedTotalCost))
                .ToList());
    }
}
