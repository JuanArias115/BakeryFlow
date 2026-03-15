using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class Production : AuditableEntity
{
    public Guid RecipeId { get; set; }

    public Recipe? Recipe { get; set; }

    public DateTime Date { get; set; }

    public decimal QuantityToProduce { get; set; }

    public decimal QuantityActual { get; set; }

    public decimal TotalCost { get; set; }

    public string? Notes { get; set; }

    public ICollection<ProductionDetail> Details { get; set; } = new List<ProductionDetail>();
}
