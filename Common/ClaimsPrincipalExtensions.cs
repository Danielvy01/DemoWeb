using System.Security.Claims;

namespace Siserp.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetEmpresaId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("empresaId");
        return Guid.Parse(value ?? throw new UnauthorizedAccessException("Token sin empresaId"));
    }

    /// <summary>Id del usuario autenticado (claim "sub", mapeado por defecto a NameIdentifier).</summary>
    public static Guid GetUsuarioId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.Parse(value ?? throw new UnauthorizedAccessException("Token sin id de usuario"));
    }
}
