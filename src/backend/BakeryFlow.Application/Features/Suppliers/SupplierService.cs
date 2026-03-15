using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Suppliers;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierDto> CreateAsync(SaveSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierDto> UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Contact,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SaveSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Contact,
    string? Notes,
    bool IsActive);

public sealed class SaveSupplierRequestValidator : AbstractValidator<SaveSupplierRequest>
{
    public SaveSupplierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(250);
        RuleFor(x => x.Contact).MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class SupplierService(IBakeryFlowDbContext dbContext) : ISupplierService
{
    public async Task<PagedResult<SupplierDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Suppliers
            .AsNoTracking()
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Search) ||
                x.Name.ToLower().Contains(request.Search.ToLower()) ||
                (x.Contact != null && x.Contact.ToLower().Contains(request.Search.ToLower())))
            .OrderBy(x => x.Name)
            .Select(x => new SupplierDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Address,
                x.Contact,
                x.Notes,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SupplierDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Address,
                x.Contact,
                x.Notes,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return supplier ?? throw new NotFoundException("Proveedor no encontrado.");
    }

    public async Task<SupplierDto> CreateAsync(SaveSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Contact = request.Contact?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive
        };

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(supplier.Id, cancellationToken);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Proveedor no encontrado.");

        supplier.Name = request.Name.Trim();
        supplier.Phone = request.Phone?.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.Address = request.Address?.Trim();
        supplier.Contact = request.Contact?.Trim();
        supplier.Notes = request.Notes?.Trim();
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Proveedor no encontrado.");

        supplier.IsActive = !supplier.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
