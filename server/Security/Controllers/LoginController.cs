using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Dtos;
using Security.Services;
using Security.Services.Authorization;

namespace Security.Controllers;

[ApiController]
public class LoginController(IUserService userService, ITokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("/login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = userService.TryLogin(request);
        if (user == null) return Unauthorized();

        return Ok(new { token = tokenService.GenerateToken(user) });
    }
}