using BakeryFlow.Domain.Common;

namespace BakeryFlow.Domain.Entities;

public sealed class Supplier : ActivatableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? Contact { get; set; }

    public string? Notes { get; set; }

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
