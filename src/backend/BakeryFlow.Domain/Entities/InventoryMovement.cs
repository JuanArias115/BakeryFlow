using BakeryFlow.Domain.Common;
using BakeryFlow.Domain.Enums;

namespace BakeryFlow.Domain.Entities;

public sealed class InventoryMovement : AuditableEntity
{
    public Guid IngredientId { get; set; }

    public Ingredient? Ingredient { get; set; }

    public InventoryMovementType Type { get; set; }

    public InventoryDocumentType DocumentType { get; set; }

    public Guid? DocumentId { get; set; }

    public DateTime Date { get; set; }

    public decimal QuantityIn { get; set; }

    public decimal QuantityOut { get; set; }

    public decimal ResultingBalance { get; set; }

    public decimal UnitCost { get; set; }

    public string? Notes { get; set; }
}
