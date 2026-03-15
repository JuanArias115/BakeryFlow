using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class Ingredient : ActivatableEntity
{
    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid UnitOfMeasureId { get; set; }

    public UnitOfMeasure? UnitOfMeasure { get; set; }

    public decimal StockCurrent { get; set; }

    public decimal StockMinimum { get; set; }

    public decimal AverageCost { get; set; }

    public string? Description { get; set; }

    public ICollection<RecipeDetail> RecipeDetails { get; set; } = new List<RecipeDetail>();

    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();

    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();

    public ICollection<ProductionDetail> ProductionDetails { get; set; } = new List<ProductionDetail>();
}
