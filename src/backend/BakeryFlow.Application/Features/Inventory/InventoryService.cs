using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Application.Common.Time;
using BakeryFlow.Domain.Entities;
using BakeryFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Inventory;

public interface IInventoryService
{
    Task<PagedResult<InventoryStockDto>> GetStocksAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryMovementDto>> GetMovementsAsync(InventoryMovementFilterRequest request, CancellationToken cancellationToken = default);
    Task<InventoryAdjustmentResultDto> CreateAdjustmentAsync(CreateInventoryAdjustmentRequest request, CancellationToken cancellationToken = default);
}

public sealed record InventoryStockDto(
    Guid IngredientId,
    string IngredientName,
    string UnitName,
    decimal StockCurrent,
    decimal StockMinimum,
    decimal AverageCost,
    bool IsLowStock);

public sealed record InventoryMovementDto(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    InventoryMovementType Type,
    InventoryDocumentType DocumentType,
    Guid? DocumentId,
    DateTime Date,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal ResultingBalance,
    decimal UnitCost,
    string? Notes,
    DateTime CreatedAt);

public sealed record InventoryAdjustmentResultDto(
    Guid MovementId,
    Guid IngredientId,
    string IngredientName,
    decimal NewStock,
    decimal AverageCost);

public sealed class InventoryMovementFilterRequest : PagedRequest
{
    public Guid? IngredientId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public sealed record CreateInventoryAdjustmentRequest(
    Guid IngredientId,
    decimal QuantityDelta,
    decimal? UnitCost,
    string? Notes,
    DateTime Date);

public sealed class InventoryMovementFilterRequestValidator : AbstractValidator<InventoryMovementFilterRequest>
{
    public InventoryMovementFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From.HasValue && x.To.HasValue);
    }
}

public sealed class CreateInventoryAdjustmentRequestValidator : AbstractValidator<CreateInventoryAdjustmentRequest>
{
    public CreateInventoryAdjustmentRequestValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.QuantityDelta).NotEqual(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
    }
}

public sealed class InventoryService(IBakeryFlowDbContext dbContext) : IInventoryService
{
    public async Task<PagedResult<InventoryStockDto>> GetStocksAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Ingredients
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Where(x =>
                string.IsNullOrWhiteSpace(term) ||
                x.Name.ToLower().Contains(term) ||
                (x.Code != null && x.Code.ToLower().Contains(term)))
            .OrderBy(x => x.Name)
            .Select(x => new InventoryStockDto(
                x.Id,
                x.Name,
                x.UnitOfMeasure!.Abbreviation,
                x.StockCurrent,
                x.StockMinimum,
                x.AverageCost,
                x.StockCurrent <= x.StockMinimum));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PagedResult<InventoryMovementDto>> GetMovementsAsync(InventoryMovementFilterRequest request, CancellationToken cancellationToken = default)
    {
        var fromUtc = UtcDateTime.EnsureUtc(request.From);
        var toUtc = UtcDateTime.EnsureUtc(request.To);
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.InventoryMovements
            .AsNoTracking()
            .Include(x => x.Ingredient)
            .Where(x =>
                (!request.IngredientId.HasValue || x.IngredientId == request.IngredientId.Value) &&
                (!fromUtc.HasValue || x.Date >= fromUtc.Value) &&
                (!toUtc.HasValue || x.Date <= toUtc.Value) &&
                (string.IsNullOrWhiteSpace(term) || x.Ingredient!.Name.ToLower().Contains(term)))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new InventoryMovementDto(
                x.Id,
                x.IngredientId,
                x.Ingredient!.Name,
                x.Type,
                x.DocumentType,
                x.DocumentId,
                x.Date,
                x.QuantityIn,
                x.QuantityOut,
                x.ResultingBalance,
                x.UnitCost,
                x.Notes,
                x.CreatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<InventoryAdjustmentResultDto> CreateAdjustmentAsync(CreateInventoryAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var ingredient = await dbContext.Ingredients
            .FirstOrDefaultAsync(x => x.Id == request.IngredientId, cancellationToken)
            ?? throw new NotFoundException("Ingrediente no encontrado.");

        var newStock = ingredient.StockCurrent + request.QuantityDelta;
        if (newStock < 0)
        {
            throw new BusinessRuleException("El ajuste dejaría el inventario en negativo.");
        }

        var movementCost = request.UnitCost ?? ingredient.AverageCost;
        if (request.QuantityDelta > 0)
        {
            ingredient.AverageCost = ingredient.StockCurrent <= 0
                ? movementCost
                : ((ingredient.StockCurrent * ingredient.AverageCost) + (request.QuantityDelta * movementCost)) / newStock;
        }

        ingredient.StockCurrent = newStock;
        ingredient.UpdatedAt = DateTime.UtcNow;

        var movement = new InventoryMovement
        {
            IngredientId = ingredient.Id,
            Type = InventoryMovementType.Adjustment,
            DocumentType = InventoryDocumentType.Adjustment,
            Date = request.Date == default ? DateTime.UtcNow : UtcDateTime.EnsureUtc(request.Date),
            QuantityIn = request.QuantityDelta > 0 ? request.QuantityDelta : 0,
            QuantityOut = request.QuantityDelta < 0 ? Math.Abs(request.QuantityDelta) : 0,
            ResultingBalance = ingredient.StockCurrent,
            UnitCost = movementCost,
            Notes = request.Notes?.Trim()
        };

        dbContext.InventoryMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new InventoryAdjustmentResultDto(movement.Id, ingredient.Id, ingredient.Name, ingredient.StockCurrent, ingredient.AverageCost);
    }
}
