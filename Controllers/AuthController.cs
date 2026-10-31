using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siserp.Api.Common;
using Siserp.Api.Dtos;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            return Ok(await _authService.LoginAsync(dto.Email, dto.Password));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        return Ok(await _authService.RegisterAsync(dto));
    }

    /// <summary>Roles y permisos efectivos del usuario autenticado (para refrescar el sidebar sin volver a iniciar sesión).</summary>
    [Authorize]
    [HttpGet("permisos")]
    public async Task<IActionResult> Permisos()
    {
        var (roles, permisos) = await _authService.ObtenerRolesYPermisosAsync(User.GetUsuarioId());
        return Ok(new { roles, permisos });
    }
}
