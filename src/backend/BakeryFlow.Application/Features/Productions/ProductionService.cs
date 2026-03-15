using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using BakeryFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Productions;

public interface IProductionService
{
    Task<PagedResult<ProductionListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ProductionDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductionPreviewDto> PreviewAsync(CreateProductionRequest request, CancellationToken cancellationToken = default);
    Task<ProductionDetailDto> CreateAsync(CreateProductionRequest request, CancellationToken cancellationToken = default);
}

public sealed record ProductionListItemDto(
    Guid Id,
    Guid RecipeId,
    string ProductName,
    DateTime Date,
    decimal QuantityToProduce,
    decimal QuantityActual,
    decimal TotalCost,
    DateTime CreatedAt);

public sealed record ProductionLineDto(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    decimal QuantityConsumed,
    decimal UnitCost,
    decimal TotalCost);

public sealed record ProductionDetailDto(
    Guid Id,
    Guid RecipeId,
    string ProductName,
    DateTime Date,
    decimal QuantityToProduce,
    decimal QuantityActual,
    decimal TotalCost,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<ProductionLineDto> Details);

public sealed record ProductionPreviewLineDto(
    Guid IngredientId,
    string IngredientName,
    decimal QuantityRequired,
    decimal StockAvailable,
    decimal UnitCost,
    decimal TotalCost);

public sealed record ProductionPreviewDto(
    Guid RecipeId,
    string ProductName,
    decimal QuantityToProduce,
    decimal QuantityActual,
    decimal PackagingCost,
    decimal TotalCost,
    IReadOnlyCollection<ProductionPreviewLineDto> Details);

public sealed record CreateProductionRequest(
    Guid RecipeId,
    DateTime Date,
    decimal QuantityToProduce,
    decimal? QuantityActual,
    string? Notes);

public sealed class CreateProductionRequestValidator : AbstractValidator<CreateProductionRequest>
{
    public CreateProductionRequestValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.QuantityToProduce).GreaterThan(0);
        RuleFor(x => x.QuantityActual).GreaterThan(0).When(x => x.QuantityActual.HasValue);
    }
}

public sealed class ProductionService(IBakeryFlowDbContext dbContext) : IProductionService
{
    private const bool AllowNegativeInventory = false;

    public async Task<PagedResult<ProductionListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Productions
            .AsNoTracking()
            .Include(x => x.Recipe!)
            .ThenInclude(x => x.Product)
            .Where(x => string.IsNullOrWhiteSpace(term) || x.Recipe!.Product!.Name.ToLower().Contains(term))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ProductionListItemDto(
                x.Id,
                x.RecipeId,
                x.Recipe!.Product!.Name,
                x.Date,
                x.QuantityToProduce,
                x.QuantityActual,
                x.TotalCost,
                x.CreatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ProductionDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var production = await dbContext.Productions
            .AsNoTracking()
            .Include(x => x.Recipe!)
                .ThenInclude(x => x.Product)
            .Include(x => x.Details)
                .ThenInclude(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return production is null ? throw new NotFoundException("Producción no encontrada.") : MapProduction(production);
    }

    public async Task<ProductionPreviewDto> PreviewAsync(CreateProductionRequest request, CancellationToken cancellationToken = default)
    {
        var recipe = await LoadRecipeAsync(request.RecipeId, cancellationToken);
        var factor = request.QuantityToProduce / recipe.Yield;
        var packagingCost = recipe.PackagingCost * factor;
        var lines = recipe.Details
            .Select(detail =>
            {
                var quantityRequired = detail.Quantity * factor;
                var unitCost = detail.Ingredient!.AverageCost;

                return new ProductionPreviewLineDto(
                    detail.IngredientId,
                    detail.Ingredient.Name,
                    quantityRequired,
                    detail.Ingredient.StockCurrent,
                    unitCost,
                    unitCost * quantityRequired);
            })
            .ToList();

        if (!AllowNegativeInventory)
        {
            var missingStock = lines.FirstOrDefault(x => x.StockAvailable < x.QuantityRequired);
            if (missingStock is not null)
            {
                throw new BusinessRuleException($"Stock insuficiente para {missingStock.IngredientName}.");
            }
        }

        return new ProductionPreviewDto(
            recipe.Id,
            recipe.Product!.Name,
            request.QuantityToProduce,
            request.QuantityActual ?? request.QuantityToProduce,
            packagingCost,
            lines.Sum(x => x.TotalCost) + packagingCost,
            lines);
    }

    public async Task<ProductionDetailDto> CreateAsync(CreateProductionRequest request, CancellationToken cancellationToken = default)
    {
        var recipe = await LoadRecipeAsync(request.RecipeId, cancellationToken);
        var preview = await PreviewAsync(request, cancellationToken);

        var production = new Production
        {
            RecipeId = recipe.Id,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            QuantityToProduce = request.QuantityToProduce,
            QuantityActual = request.QuantityActual ?? request.QuantityToProduce,
            TotalCost = preview.TotalCost,
            Notes = request.Notes?.Trim()
        };

        production.Details = preview.Details.Select(detail =>
        {
            var ingredient = recipe.Details.First(x => x.IngredientId == detail.IngredientId).Ingredient!;
            ingredient.StockCurrent -= detail.QuantityRequired;
            ingredient.UpdatedAt = DateTime.UtcNow;

            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                IngredientId = ingredient.Id,
                Type = InventoryMovementType.Production,
                DocumentType = InventoryDocumentType.Production,
                DocumentId = production.Id,
                Date = production.Date,
                QuantityIn = 0,
                QuantityOut = detail.QuantityRequired,
                ResultingBalance = ingredient.StockCurrent,
                UnitCost = detail.UnitCost,
                Notes = $"Producción de {recipe.Product!.Name}"
            });

            return new ProductionDetail
            {
                IngredientId = detail.IngredientId,
                QuantityConsumed = detail.QuantityRequired,
                UnitCost = detail.UnitCost,
                TotalCost = detail.TotalCost
            };
        }).ToList();

        dbContext.Productions.Add(production);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(production.Id, cancellationToken);
    }

    private async Task<Recipe> LoadRecipeAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes
            .Include(x => x.Product)
            .Include(x => x.Details)
                .ThenInclude(x => x.Ingredient)
            .FirstOrDefaultAsync(x => x.Id == recipeId, cancellationToken)
            ?? throw new NotFoundException("Receta no encontrada.");

        if (!recipe.IsActive)
        {
            throw new BusinessRuleException("Solo se puede producir con una receta activa.");
        }

        return recipe;
    }

    private static ProductionDetailDto MapProduction(Production production) =>
        new(
            production.Id,
            production.RecipeId,
            production.Recipe?.Product?.Name ?? string.Empty,
            production.Date,
            production.QuantityToProduce,
            production.QuantityActual,
            production.TotalCost,
            production.Notes,
            production.CreatedAt,
            production.UpdatedAt,
            production.Details.Select(x => new ProductionLineDto(
                x.Id,
                x.IngredientId,
                x.Ingredient?.Name ?? string.Empty,
                x.QuantityConsumed,
                x.UnitCost,
                x.TotalCost)).ToList());
}
