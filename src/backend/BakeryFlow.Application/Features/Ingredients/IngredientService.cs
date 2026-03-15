using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using BakeryFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Ingredients;

public interface IIngredientService
{
    Task<PagedResult<IngredientDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<IngredientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IngredientDto> CreateAsync(SaveIngredientRequest request, CancellationToken cancellationToken = default);
    Task<IngredientDto> UpdateAsync(Guid id, SaveIngredientRequest request, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record IngredientDto(
    Guid Id,
    string? Code,
    string Name,
    Guid UnitOfMeasureId,
    string UnitName,
    decimal StockCurrent,
    decimal StockMinimum,
    decimal AverageCost,
    string? Description,
    bool IsActive,
    bool IsLowStock,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SaveIngredientRequest(
    string? Code,
    string Name,
    Guid UnitOfMeasureId,
    decimal StockCurrent,
    decimal StockMinimum,
    decimal AverageCost,
    string? Description,
    bool IsActive);

public sealed class SaveIngredientRequestValidator : AbstractValidator<SaveIngredientRequest>
{
    public SaveIngredientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.UnitOfMeasureId).NotEmpty();
        RuleFor(x => x.StockCurrent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockMinimum).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AverageCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Code).MaximumLength(40);
    }
}

public sealed class IngredientService(IBakeryFlowDbContext dbContext) : IIngredientService
{
    public async Task<PagedResult<IngredientDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Ingredients
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Where(x =>
                string.IsNullOrWhiteSpace(term) ||
                x.Name.ToLower().Contains(term) ||
                (x.Code != null && x.Code.ToLower().Contains(term)) ||
                x.UnitOfMeasure!.Name.ToLower().Contains(term))
            .OrderBy(x => x.Name)
            .Select(x => new IngredientDto(
                x.Id,
                x.Code,
                x.Name,
                x.UnitOfMeasureId,
                $"{x.UnitOfMeasure!.Name} ({x.UnitOfMeasure.Abbreviation})",
                x.StockCurrent,
                x.StockMinimum,
                x.AverageCost,
                x.Description,
                x.IsActive,
                x.StockCurrent <= x.StockMinimum,
                x.CreatedAt,
                x.UpdatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Ingredients
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<IngredientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ingredient = await dbContext.Ingredients
            .AsNoTracking()
            .Include(x => x.UnitOfMeasure)
            .Where(x => x.Id == id)
            .Select(x => new IngredientDto(
                x.Id,
                x.Code,
                x.Name,
                x.UnitOfMeasureId,
                $"{x.UnitOfMeasure!.Name} ({x.UnitOfMeasure.Abbreviation})",
                x.StockCurrent,
                x.StockMinimum,
                x.AverageCost,
                x.Description,
                x.IsActive,
                x.StockCurrent <= x.StockMinimum,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return ingredient ?? throw new NotFoundException("Ingrediente no encontrado.");
    }

    public async Task<IngredientDto> CreateAsync(SaveIngredientRequest request, CancellationToken cancellationToken = default)
    {
        var unitExists = await dbContext.UnitsOfMeasure.AnyAsync(x => x.Id == request.UnitOfMeasureId, cancellationToken);
        if (!unitExists)
        {
            throw new BusinessRuleException("La unidad de medida seleccionada no existe.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var duplicatedCode = await dbContext.Ingredients.AnyAsync(x => x.Code == request.Code.Trim(), cancellationToken);
            if (duplicatedCode)
            {
                throw new BusinessRuleException("Ya existe un ingrediente con ese código.");
            }
        }

        var ingredient = new Ingredient
        {
            Code = request.Code?.Trim(),
            Name = request.Name.Trim(),
            UnitOfMeasureId = request.UnitOfMeasureId,
            StockCurrent = 0,
            StockMinimum = request.StockMinimum,
            AverageCost = request.AverageCost,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };

        dbContext.Ingredients.Add(ingredient);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.StockCurrent > 0)
        {
            ingredient.StockCurrent = request.StockCurrent;
            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                IngredientId = ingredient.Id,
                Type = InventoryMovementType.Adjustment,
                DocumentType = InventoryDocumentType.Adjustment,
                Date = DateTime.UtcNow,
                QuantityIn = request.StockCurrent,
                QuantityOut = 0,
                ResultingBalance = ingredient.StockCurrent,
                UnitCost = ingredient.AverageCost,
                Notes = "Stock inicial"
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetByIdAsync(ingredient.Id, cancellationToken);
    }

    public async Task<IngredientDto> UpdateAsync(Guid id, SaveIngredientRequest request, CancellationToken cancellationToken = default)
    {
        var ingredient = await dbContext.Ingredients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Ingrediente no encontrado.");

        var unitExists = await dbContext.UnitsOfMeasure.AnyAsync(x => x.Id == request.UnitOfMeasureId, cancellationToken);
        if (!unitExists)
        {
            throw new BusinessRuleException("La unidad de medida seleccionada no existe.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var duplicatedCode = await dbContext.Ingredients.AnyAsync(
                x => x.Id != id && x.Code == request.Code.Trim(),
                cancellationToken);

            if (duplicatedCode)
            {
                throw new BusinessRuleException("Ya existe un ingrediente con ese código.");
            }
        }

        ingredient.Code = request.Code?.Trim();
        ingredient.Name = request.Name.Trim();
        ingredient.UnitOfMeasureId = request.UnitOfMeasureId;
        ingredient.StockMinimum = request.StockMinimum;
        ingredient.AverageCost = request.AverageCost;
        ingredient.Description = request.Description?.Trim();
        ingredient.IsActive = request.IsActive;
        ingredient.UpdatedAt = DateTime.UtcNow;

        var delta = request.StockCurrent - ingredient.StockCurrent;
        if (delta < 0 && ingredient.StockCurrent + delta < 0)
        {
            throw new BusinessRuleException("El cambio de stock dejaría el inventario en negativo.");
        }

        if (delta != 0)
        {
            ingredient.StockCurrent = request.StockCurrent;
            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                IngredientId = ingredient.Id,
                Type = InventoryMovementType.Adjustment,
                DocumentType = InventoryDocumentType.Adjustment,
                Date = DateTime.UtcNow,
                QuantityIn = delta > 0 ? delta : 0,
                QuantityOut = delta < 0 ? Math.Abs(delta) : 0,
                ResultingBalance = ingredient.StockCurrent,
                UnitCost = ingredient.AverageCost,
                Notes = "Ajuste desde maestro"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ingredient = await dbContext.Ingredients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Ingrediente no encontrado.");

        ingredient.IsActive = !ingredient.IsActive;
        ingredient.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
