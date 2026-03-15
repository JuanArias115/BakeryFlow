using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using BakeryFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Sales;

public interface ISaleService
{
    Task<PagedResult<SaleListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<SaleDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SaleDetailDto> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
}

public sealed record SaleListItemDto(
    Guid Id,
    string? CustomerName,
    DateTime Date,
    decimal Subtotal,
    decimal Total,
    decimal Profit,
    PaymentMethod PaymentMethod,
    SaleStatus Status,
    DateTime CreatedAt);

public sealed record SaleLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal UnitCost,
    decimal Profit);

public sealed record SaleDetailDto(
    Guid Id,
    Guid? CustomerId,
    string? CustomerName,
    DateTime Date,
    string? Notes,
    decimal Subtotal,
    decimal Total,
    decimal Profit,
    PaymentMethod PaymentMethod,
    SaleStatus Status,
    DateTime CreatedAt,
    IReadOnlyCollection<SaleLineDto> Details);

public sealed record CreateSaleLineRequest(Guid ProductId, string? Description, decimal Quantity, decimal? UnitPrice);

public sealed record CreateSaleRequest(
    Guid? CustomerId,
    DateTime Date,
    string? Notes,
    PaymentMethod PaymentMethod,
    IReadOnlyCollection<CreateSaleLineRequest> Details);

public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.Details).NotEmpty();
        RuleForEach(x => x.Details).ChildRules(detail =>
        {
            detail.RuleFor(x => x.ProductId).NotEmpty();
            detail.RuleFor(x => x.Quantity).GreaterThan(0);
            detail.RuleFor(x => x.UnitPrice).GreaterThan(0).When(x => x.UnitPrice.HasValue);
        });
    }
}

public sealed class SaleService(IBakeryFlowDbContext dbContext) : ISaleService
{
    public async Task<PagedResult<SaleListItemDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Details)
            .Where(x =>
                string.IsNullOrWhiteSpace(term) ||
                (x.Customer != null && x.Customer.Name.ToLower().Contains(term)))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new SaleListItemDto(
                x.Id,
                x.Customer != null ? x.Customer.Name : null,
                x.Date,
                x.Subtotal,
                x.Total,
                x.Details.Sum(d => d.Profit),
                x.PaymentMethod,
                x.Status,
                x.CreatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<SaleDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await dbContext.Sales
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Details)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return sale is null ? throw new NotFoundException("Venta no encontrada.") : MapSale(sale);
    }

    public async Task<SaleDetailDto> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId.HasValue)
        {
            var customerExists = await dbContext.Customers.AnyAsync(x => x.Id == request.CustomerId.Value, cancellationToken);
            if (!customerExists)
            {
                throw new BusinessRuleException("El cliente seleccionado no existe.");
            }
        }

        var details = await BuildDetailsAsync(request.Details, cancellationToken);
        var sale = new Sale
        {
            CustomerId = request.CustomerId,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            Notes = request.Notes?.Trim(),
            PaymentMethod = request.PaymentMethod,
            Status = SaleStatus.Completed,
            Subtotal = details.Sum(x => x.Subtotal),
            Total = details.Sum(x => x.Subtotal),
            Details = details
        };

        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    private async Task<List<SaleDetail>> BuildDetailsAsync(
        IReadOnlyCollection<CreateSaleLineRequest> detailRequests,
        CancellationToken cancellationToken)
    {
        var productIds = detailRequests.Select(x => x.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new BusinessRuleException("Uno o más productos no existen.");
        }

        var activeRecipes = await dbContext.Recipes
            .Include(x => x.Details)
            .Include(x => x.Product)
            .Where(x => productIds.Contains(x.ProductId) && x.IsActive)
            .ToListAsync(cancellationToken);

        return detailRequests.Select(detail =>
        {
            var product = products.First(x => x.Id == detail.ProductId);
            var recipe = activeRecipes.FirstOrDefault(x => x.ProductId == detail.ProductId)
                ?? throw new BusinessRuleException($"El producto {product.Name} no tiene receta activa.");

            var unitCost = (recipe.Details.Sum(x => x.CalculatedTotalCost) + recipe.PackagingCost) / recipe.Yield;
            var unitPrice = detail.UnitPrice ?? product.SalePrice;
            var subtotal = unitPrice * detail.Quantity;
            var totalCost = unitCost * detail.Quantity;

            return new SaleDetail
            {
                ProductId = product.Id,
                Description = string.IsNullOrWhiteSpace(detail.Description) ? product.Name : detail.Description.Trim(),
                Quantity = detail.Quantity,
                UnitPrice = unitPrice,
                Subtotal = subtotal,
                UnitCost = unitCost,
                Profit = subtotal - totalCost
            };
        }).ToList();
    }

    private static SaleDetailDto MapSale(Sale sale) =>
        new(
            sale.Id,
            sale.CustomerId,
            sale.Customer?.Name,
            sale.Date,
            sale.Notes,
            sale.Subtotal,
            sale.Total,
            sale.Details.Sum(x => x.Profit),
            sale.PaymentMethod,
            sale.Status,
            sale.CreatedAt,
            sale.Details.Select(x => new SaleLineDto(
                x.Id,
                x.ProductId,
                x.Product?.Name ?? string.Empty,
                x.Description,
                x.Quantity,
                x.UnitPrice,
                x.Subtotal,
                x.UnitCost,
                x.Profit)).ToList());
}
