using System.Security.Claims;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Dtos;
using Security.Services.Authorization;
using Security.Services.Security;

namespace Security.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(IUserRepository users, IPasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        if (GetActor() == null) return Unauthorized();

        return Ok(users.GetAll().Select(ToResponse));
    }

    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var actor = GetActor();
        if (actor == null) return Unauthorized();

        return Ok(ToResponse(actor));
    }


    
    [HttpPost]
    public IActionResult Create([FromBody] CreateUserRequest request)
    {
        var actor = GetActor();
        if (actor == null) return Unauthorized();
        if (!RoleRules.CanChange(actor.Role, request.Role)) return Forbid();

        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHasher.HashAndSaltPassword(request.Password),
            Role = request.Role
        };
        if (!users.Create(user)) return Conflict("That username is taken.");

        return Created($"/api/users/{user.Id}", ToResponse(user));
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] UpdateUserRequest request)
    {
        var actor = GetActor();
        if (actor == null) return Unauthorized();

        var target = users.GetById(id);
        if (target == null) return NotFound();

        // Check the role they have now and the role they would get,
        // so a superuser can't give a higher role.
        if (!RoleRules.CanChange(actor.Role, target.Role)) return Forbid();
        if (!RoleRules.CanChange(actor.Role, request.Role)) return Forbid();

        // Nobody changes their own role, so there is always an admin left.
        if (target.Id == actor.Id && target.Role != request.Role) return Forbid();

        var sameName = users.GetByUsername(request.Username);
        if (sameName != null && sameName.Id != id) return Conflict("That username is taken.");

        target.Username = request.Username;
        target.Role = request.Role;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            target.PasswordHash = passwordHasher.HashAndSaltPassword(request.Password);
        }
        users.Update(target);

        return Ok(ToResponse(target));
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var actor = GetActor();
        if (actor == null) return Unauthorized();

        var target = users.GetById(id);
        if (target == null) return NotFound();
        if (!RoleRules.CanChange(actor.Role, target.Role)) return Forbid();
        if (target.Id == actor.Id) return Forbid();

        users.Delete(target);
        return NoContent();
    }

    // Read the logged-in user from the database, not from the token,
    // so a changed role or a deleted account counts right away.
    private User? GetActor()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return users.GetById(id);
    }

    private static UserResponse ToResponse(User user) => new(user.Id, user.Username, user.Role);
}
