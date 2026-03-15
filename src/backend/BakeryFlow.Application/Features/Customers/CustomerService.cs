using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(Guid id, SaveCustomerRequest request, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SaveCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive);

public sealed class SaveCustomerRequestValidator : AbstractValidator<SaveCustomerRequest>
{
    public SaveCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Address).MaximumLength(250);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class CustomerService(IBakeryFlowDbContext dbContext) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers
            .AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(request.Search) || x.Name.ToLower().Contains(request.Search.ToLower()))
            .OrderBy(x => x.Name)
            .Select(x => new CustomerDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Address,
                x.Notes,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CustomerDto(
                x.Id,
                x.Name,
                x.Phone,
                x.Email,
                x.Address,
                x.Notes,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return customer ?? throw new NotFoundException("Cliente no encontrado.");
    }

    public async Task<CustomerDto> CreateAsync(SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = request.IsActive
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(customer.Id, cancellationToken);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, SaveCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        customer.Name = request.Name.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Email = request.Email?.Trim();
        customer.Address = request.Address?.Trim();
        customer.Notes = request.Notes?.Trim();
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        customer.IsActive = !customer.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
