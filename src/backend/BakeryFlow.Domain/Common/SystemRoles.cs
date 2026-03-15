namespace BakeryFlow.Domain.Common;

public static class SystemRoles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";

    public static readonly IReadOnlyCollection<string> All = [Admin, Operator];
}
