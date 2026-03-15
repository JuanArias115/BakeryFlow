using BakeryFlow.Domain.Common;
using BakeryFlow.Domain.Enums;

namespace BakeryFlow.Domain.Entities;

public sealed class Purchase : AuditableEntity
{
    public Guid SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime PurchaseDate { get; set; }

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

    public ICollection<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
}
