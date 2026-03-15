namespace BakeryFlow.Domain.Common;

public abstract class ActivatableEntity : AuditableEntity
{
    public bool IsActive { get; set; } = true;
}
