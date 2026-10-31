using Microsoft.AspNetCore.Mvc.Filters;
using Siserp.Api.Common;
using Siserp.Api.Models;
using Siserp.Api.Services;

namespace Siserp.Api.Infrastructure.Authorization;

/// <summary>
/// Autoriza el acceso a un controlador/acción según el permiso que el usuario autenticado
/// tenga sobre un menú del catálogo (<see cref="MenuCatalogo"/>), calculado como el máximo
/// nivel entre todos sus roles (ver <see cref="IPermissionService"/>).
///
/// El nivel requerido se infiere del verbo HTTP salvo que se indique explícitamente:
/// GET/HEAD → Lectura; POST/PUT/PATCH → Escritura; DELETE → Control Total.
/// Debe usarse junto con <c>[Authorize]</c> (requiere que el usuario ya esté autenticado).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class MenuPermisoAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _menuCodigo;
    private readonly NivelAcceso? _nivelRequerido;

    public MenuPermisoAttribute(string menuCodigo, NivelAcceso nivelRequerido)
    {
        _menuCodigo = menuCodigo;
        _nivelRequerido = nivelRequerido;
    }

    public MenuPermisoAttribute(string menuCodigo)
    {
        _menuCodigo = menuCodigo;
        _nivelRequerido = null;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity is not { IsAuthenticated: true })
        {
            // Deja que [Authorize] (que se ejecuta en el mismo pipeline) responda 401.
            return;
        }

        var nivelRequerido = _nivelRequerido ?? NivelRequeridoSegunVerbo(context.HttpContext.Request.Method);

        Guid usuarioId;
        try
        {
            usuarioId = user.GetUsuarioId();
        }
        catch (UnauthorizedAccessException)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult(new { message = "Token sin id de usuario" });
            return;
        }

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var nivelActual = await permissionService.GetNivelAsync(usuarioId, _menuCodigo);

        if (nivelActual < nivelRequerido)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                message = $"No tiene permiso suficiente sobre el menú '{_menuCodigo}' (requiere {nivelRequerido}, tiene {nivelActual}).",
            })
            { StatusCode = 403 };
        }
    }

    private static NivelAcceso NivelRequeridoSegunVerbo(string metodo) => metodo.ToUpperInvariant() switch
    {
        "GET" or "HEAD" => NivelAcceso.LECTURA,
        "DELETE" => NivelAcceso.CONTROL_TOTAL,
        _ => NivelAcceso.ESCRITURA,
    };
}
