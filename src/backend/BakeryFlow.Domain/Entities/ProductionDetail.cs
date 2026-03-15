using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class ProductionDetail : BaseEntity
{
    public Guid ProductionId { get; set; }

    public Production? Production { get; set; }

    public Guid IngredientId { get; set; }

    public Ingredient? Ingredient { get; set; }

    public decimal QuantityConsumed { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TotalCost { get; set; }
}
