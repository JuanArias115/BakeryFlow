using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class RecipeDetail : BaseEntity
{
    public Guid RecipeId { get; set; }

    public Recipe? Recipe { get; set; }

    public Guid IngredientId { get; set; }

    public Ingredient? Ingredient { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitOfMeasureId { get; set; }

    public UnitOfMeasure? UnitOfMeasure { get; set; }

    public decimal CalculatedUnitCost { get; set; }

    public decimal CalculatedTotalCost { get; set; }
}
