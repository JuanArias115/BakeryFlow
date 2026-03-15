using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(SaveProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, SaveProductRequest request, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record ProductDto(
    Guid Id,
    string? Code,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string UnitSale,
    decimal SalePrice,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SaveProductRequest(
    string? Code,
    string Name,
    Guid CategoryId,
    string UnitSale,
    decimal SalePrice,
    string? Description,
    bool IsActive);

public sealed class SaveProductRequestValidator : AbstractValidator<SaveProductRequest>
{
    public SaveProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.UnitSale).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SalePrice).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Code).MaximumLength(40);
    }
}

public sealed class ProductService(IBakeryFlowDbContext dbContext) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var term = request.Search?.Trim().ToLower();
        var query = dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x =>
                string.IsNullOrWhiteSpace(term) ||
                x.Name.ToLower().Contains(term) ||
                (x.Code != null && x.Code.ToLower().Contains(term)) ||
                x.Category!.Name.ToLower().Contains(term))
            .OrderBy(x => x.Name)
            .Select(x => new ProductDto(
                x.Id,
                x.Code,
                x.Name,
                x.CategoryId,
                x.Category!.Name,
                x.UnitSale,
                x.SalePrice,
                x.Description,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.Id == id)
            .Select(x => new ProductDto(
                x.Id,
                x.Code,
                x.Name,
                x.CategoryId,
                x.Category!.Name,
                x.UnitSale,
                x.SalePrice,
                x.Description,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException("Producto no encontrado.");
    }

    public async Task<ProductDto> CreateAsync(SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        var categoryExists = await dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new BusinessRuleException("La categoría seleccionada no existe.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var duplicatedCode = await dbContext.Products.AnyAsync(x => x.Code == request.Code.Trim(), cancellationToken);
            if (duplicatedCode)
            {
                throw new BusinessRuleException("Ya existe un producto con ese código.");
            }
        }

        var product = new Product
        {
            Code = request.Code?.Trim(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            UnitSale = request.UnitSale.Trim(),
            SalePrice = request.SalePrice,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, SaveProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Producto no encontrado.");

        var categoryExists = await dbContext.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new BusinessRuleException("La categoría seleccionada no existe.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var duplicatedCode = await dbContext.Products.AnyAsync(
                x => x.Id != id && x.Code == request.Code.Trim(),
                cancellationToken);

            if (duplicatedCode)
            {
                throw new BusinessRuleException("Ya existe un producto con ese código.");
            }
        }

        product.Code = request.Code?.Trim();
        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.UnitSale = request.UnitSale.Trim();
        product.SalePrice = request.SalePrice;
        product.Description = request.Description?.Trim();
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Producto no encontrado.");

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
