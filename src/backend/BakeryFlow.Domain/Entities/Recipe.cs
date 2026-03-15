using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class Recipe : AuditableEntity
{
    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal Yield { get; set; }

    public string YieldUnit { get; set; } = string.Empty;

    public decimal PackagingCost { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RecipeDetail> Details { get; set; } = new List<RecipeDetail>();

    public ICollection<Production> Productions { get; set; } = new List<Production>();
}
