using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using BakeryFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Purchases;

public interface IPurchaseService
{
    Task<PagedResult<PurchaseListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseDetailDto> CreateAsync(SavePurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseDetailDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record PurchaseListItemDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string? InvoiceNumber,
    DateTime PurchaseDate,
    decimal Subtotal,
    decimal Total,
    PurchaseStatus Status,
    DateTime CreatedAt);

public sealed record PurchaseLineDto(
    Guid Id,
    Guid IngredientId,
    string IngredientName,
    string Description,
    decimal Quantity,
    Guid UnitOfMeasureId,
    string UnitName,
    decimal UnitCost,
    decimal Subtotal);

public sealed record PurchaseDetailDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string? InvoiceNumber,
    DateTime PurchaseDate,
    string? Notes,
    decimal Subtotal,
    decimal Total,
    PurchaseStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<PurchaseLineDto> Details);

public sealed record SavePurchaseLineRequest(
    Guid IngredientId,
    string Description,
    decimal Quantity,
    Guid UnitOfMeasureId,
    decimal UnitCost);

public sealed record SavePurchaseRequest(
    Guid SupplierId,
    string? InvoiceNumber,
    DateTime PurchaseDate,
    string? Notes,
    IReadOnlyCollection<SavePurchaseLineRequest> Details);

public sealed class SavePurchaseRequestValidator : AbstractValidator<SavePurchaseRequest>
{
    public SavePurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(x => x.IngredientId).NotEmpty();
            detail.RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            detail.RuleFor(x => x.Quantity).GreaterThan(0);
            detail.RuleFor(x => x.UnitOfMeasureId).NotEmpty();
            detail.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class PurchaseService(IBakeryFlowDbContext dbContext) : IPurchaseService
{
    public async Task<PagedResult<PurchaseListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Purchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Where(x =>
                string.IsNullOrWhiteSpace(term) ||
                x.Supplier!.Name.ToLower().Contains(term) ||
                (x.InvoiceNumber != null && x.InvoiceNumber.ToLower().Contains(term)))
            .OrderByDescending(x => x.PurchaseDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new PurchaseListItemDto(
                x.Id,
                x.SupplierId,
                x.Supplier!.Name,
                x.InvoiceNumber,
                x.PurchaseDate,
                x.Subtotal,
                x.Total,
                x.Status,
                x.CreatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<PurchaseDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var purchase = await dbContext.Purchases
            .AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(x => x.Ingredient)
            .Include(x => x.Details)
                .ThenInclude(x => x.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return purchase is null ? throw new NotFoundException("Compra no encontrada.") : MapPurchase(purchase);
    }

    public async Task<PurchaseDetailDto> CreateAsync(SavePurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var supplierExists = await dbContext.Suppliers.AnyAsync(x => x.Id == request.SupplierId, cancellationToken);
        if (!supplierExists)
        {
            throw new BusinessRuleException("El proveedor seleccionado no existe.");
        }

        var purchase = new Purchase
        {
            SupplierId = request.SupplierId,
            InvoiceNumber = request.InvoiceNumber?.Trim(),
            PurchaseDate = request.PurchaseDate == default ? DateTime.UtcNow : request.PurchaseDate,
            Notes = request.Notes?.Trim(),
            Status = PurchaseStatus.Draft
        };

        purchase.Details = await BuildDetailsAsync(request.Details, cancellationToken);
        purchase.Subtotal = purchase.Details.Sum(x => x.Subtotal);
        purchase.Total = purchase.Subtotal;

        dbContext.Purchases.Add(purchase);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(purchase.Id, cancellationToken);
    }

    public async Task<PurchaseDetailDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var purchase = await dbContext.Purchases
            .Include(x => x.Details)
            .ThenInclude(x => x.Ingredient)
            .Include(x => x.Supplier)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Compra no encontrada.");

        if (purchase.Status == PurchaseStatus.Confirmed)
        {
            throw new BusinessRuleException("La compra ya fue confirmada.");
        }

        foreach (var detail in purchase.Details)
        {
            var ingredient = detail.Ingredient ?? throw new BusinessRuleException("Detalle de compra inválido.");
            var previousStock = ingredient.StockCurrent;
            var newStock = previousStock + detail.Quantity;

            ingredient.AverageCost = previousStock <= 0
                ? detail.UnitCost
                : ((previousStock * ingredient.AverageCost) + (detail.Quantity * detail.UnitCost)) / newStock;

            ingredient.StockCurrent = newStock;
            ingredient.UpdatedAt = DateTime.UtcNow;

            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                IngredientId = ingredient.Id,
                Type = InventoryMovementType.Purchase,
                DocumentType = InventoryDocumentType.Purchase,
                DocumentId = purchase.Id,
                Date = purchase.PurchaseDate,
                QuantityIn = detail.Quantity,
                QuantityOut = 0,
                ResultingBalance = ingredient.StockCurrent,
                UnitCost = detail.UnitCost,
                Notes = $"Compra confirmada {purchase.InvoiceNumber}".Trim()
            });
        }

        purchase.Status = PurchaseStatus.Confirmed;
        purchase.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<List<PurchaseDetail>> BuildDetailsAsync(
        IReadOnlyCollection<SavePurchaseLineRequest> details,
        CancellationToken cancellationToken)
    {
        var ingredientIds = details.Select(x => x.IngredientId).Distinct().ToList();
        var unitIds = details.Select(x => x.UnitOfMeasureId).Distinct().ToList();

        var ingredients = await dbContext.Ingredients
            .Where(x => ingredientIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var units = await dbContext.UnitsOfMeasure
            .Where(x => unitIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (ingredients.Count != ingredientIds.Count || units.Count != unitIds.Count)
        {
            throw new BusinessRuleException("Uno o más ingredientes o unidades no existen.");
        }

        return details.Select(detail => new PurchaseDetail
        {
            IngredientId = detail.IngredientId,
            Description = detail.Description.Trim(),
            Quantity = detail.Quantity,
            UnitOfMeasureId = detail.UnitOfMeasureId,
            UnitCost = detail.UnitCost,
            Subtotal = detail.Quantity * detail.UnitCost
        }).ToList();
    }

    private static PurchaseDetailDto MapPurchase(Purchase purchase) =>
        new(
            purchase.Id,
            purchase.SupplierId,
            purchase.Supplier?.Name ?? string.Empty,
            purchase.InvoiceNumber,
            purchase.PurchaseDate,
            purchase.Notes,
            purchase.Subtotal,
            purchase.Total,
            purchase.Status,
            purchase.CreatedAt,
            purchase.UpdatedAt,
            purchase.Details
                .OrderBy(x => x.Description)
                .Select(x => new PurchaseLineDto(
                    x.Id,
                    x.IngredientId,
                    x.Ingredient?.Name ?? string.Empty,
                    x.Description,
                    x.Quantity,
                    x.UnitOfMeasureId,
                    x.UnitOfMeasure?.Abbreviation ?? string.Empty,
                    x.UnitCost,
                    x.Subtotal))
                .ToList());
}
