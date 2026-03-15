using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class PurchaseDetail : BaseEntity
{
    public Guid PurchaseId { get; set; }

    public Purchase? Purchase { get; set; }

    public Guid IngredientId { get; set; }

    public Ingredient? Ingredient { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public Guid UnitOfMeasureId { get; set; }

    public UnitOfMeasure? UnitOfMeasure { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Subtotal { get; set; }
}
