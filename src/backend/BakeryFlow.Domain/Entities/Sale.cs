using BakeryFlow.Domain.Common;
using BakeryFlow.Domain.Enums;

namespace BakeryFlow.Domain.Entities;

public sealed class Sale : AuditableEntity
{
    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTime Date { get; set; }

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();
}
