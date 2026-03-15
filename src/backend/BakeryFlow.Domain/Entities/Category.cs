using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class Category : ActivatableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
