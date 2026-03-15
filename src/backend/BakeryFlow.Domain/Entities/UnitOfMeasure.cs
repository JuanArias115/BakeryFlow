using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class UnitOfMeasure : ActivatableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public string? Type { get; set; }

    public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}
