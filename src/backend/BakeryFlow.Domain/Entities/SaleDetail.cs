using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class SaleDetail : BaseEntity
{
    public Guid SaleId { get; set; }

    public Sale? Sale { get; set; }

    public Guid ProductId { get; set; }

    public Product? Product { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Profit { get; set; }
}
