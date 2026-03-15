using BakeryFlow.Application.Common.Dtos;
using BakeryFlow.Application.Common.Exceptions;
using BakeryFlow.Application.Common.Extensions;
using BakeryFlow.Application.Common.Interfaces;
using BakeryFlow.Application.Common.Models;
using BakeryFlow.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BakeryFlow.Application.Features.Units;

public interface IUnitService
{
    Task<PagedResult<UnitDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);
    Task<UnitDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UnitDto> CreateAsync(SaveUnitRequest request, CancellationToken cancellationToken = default);
    Task<UnitDto> UpdateAsync(Guid id, SaveUnitRequest request, CancellationToken cancellationToken = default);
    Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record UnitDto(
    Guid Id,
    string Name,
    string Abbreviation,
    string? Type,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record SaveUnitRequest(string Name, string Abbreviation, string? Type, bool IsActive);

public sealed class SaveUnitRequestValidator : AbstractValidator<SaveUnitRequest>
{
    public SaveUnitRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Abbreviation).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Type).MaximumLength(50);
    }
}

public sealed class UnitService(IBakeryFlowDbContext dbContext) : IUnitService
{
    public async Task<PagedResult<UnitDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UnitsOfMeasure
            .AsNoTracking()
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Search) ||
                x.Name.ToLower().Contains(request.Search.ToLower()) ||
                x.Abbreviation.ToLower().Contains(request.Search.ToLower()))
            .OrderBy(x => x.Name)
            .Select(x => new UnitDto(x.Id, x.Name, x.Abbreviation, x.Type, x.IsActive, x.CreatedAt, x.UpdatedAt));

        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.UnitsOfMeasure
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new OptionDto(x.Id, $"{x.Name} ({x.Abbreviation})"))
            .ToListAsync(cancellationToken);

    public async Task<UnitDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.UnitsOfMeasure
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new UnitDto(x.Id, x.Name, x.Abbreviation, x.Type, x.IsActive, x.CreatedAt, x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return unit ?? throw new NotFoundException("Unidad de medida no encontrada.");
    }

    public async Task<UnitDto> CreateAsync(SaveUnitRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.UnitsOfMeasure.AnyAsync(
            x => x.Name.ToLower() == request.Name.Trim().ToLower() || x.Abbreviation.ToLower() == request.Abbreviation.Trim().ToLower(),
            cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException("Ya existe una unidad con ese nombre o abreviatura.");
        }

        var unit = new UnitOfMeasure
        {
            Name = request.Name.Trim(),
            Abbreviation = request.Abbreviation.Trim(),
            Type = request.Type?.Trim(),
            IsActive = request.IsActive
        };

        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UnitDto(unit.Id, unit.Name, unit.Abbreviation, unit.Type, unit.IsActive, unit.CreatedAt, unit.UpdatedAt);
    }

    public async Task<UnitDto> UpdateAsync(Guid id, SaveUnitRequest request, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.UnitsOfMeasure.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Unidad de medida no encontrada.");

        var exists = await dbContext.UnitsOfMeasure.AnyAsync(
            x => x.Id != id &&
                 (x.Name.ToLower() == request.Name.Trim().ToLower() ||
                  x.Abbreviation.ToLower() == request.Abbreviation.Trim().ToLower()),
            cancellationToken);

        if (exists)
        {
            throw new BusinessRuleException("Ya existe una unidad con ese nombre o abreviatura.");
        }

        unit.Name = request.Name.Trim();
        unit.Abbreviation = request.Abbreviation.Trim();
        unit.Type = request.Type?.Trim();
        unit.IsActive = request.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UnitDto(unit.Id, unit.Name, unit.Abbreviation, unit.Type, unit.IsActive, unit.CreatedAt, unit.UpdatedAt);
    }

    public async Task ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await dbContext.UnitsOfMeasure.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Unidad de medida no encontrada.");

        unit.IsActive = !unit.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
