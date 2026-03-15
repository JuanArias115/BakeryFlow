using System.Security.Claims;
using BakeryFlow.Application.Common.Exceptions;

namespace BakeryFlow.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new BusinessRuleException("Token de usuario inválido.");
    }
}
