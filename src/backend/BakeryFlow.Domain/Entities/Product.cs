using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class Product : ActivatableEntity
{
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }

    public string UnitSale { get; set; } = string.Empty;

    public decimal SalePrice { get; set; }

    public string? Description { get; set; }

    public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
}
