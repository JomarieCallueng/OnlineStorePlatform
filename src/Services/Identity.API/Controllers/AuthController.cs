using System.Security.Claims;
using Asp.Versioning;
using Identity.API.Data;
using Identity.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Store.Shared.Common.Errors;
using Store.Shared.Common.Security;

namespace Identity.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("auth/v{version:apiVersion}")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly JwtOptions _jwt;

    public AuthController(IdentityDbContext db, IPasswordHasher<AppUser> hasher, IOptions<JwtOptions> jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt.Value;
    }

    /// <summary>Register a new customer and return a JWT.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            throw new StoreValidationException("Username is required and password must be at least 6 characters.");

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            throw new ConflictException("Username is already taken.");

        var user = new AppUser { Username = req.Username.Trim(), Role = Roles.Customer };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(Issue(user));
    }

    /// <summary>Log in and return a JWT.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid username or password.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid username or password.");

        return Ok(Issue(user));
    }

    /// <summary>Return the identity of the current token holder.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        username = User.Identity?.Name,
        role = User.FindFirstValue(ClaimTypes.Role)
    });

    private AuthResponse Issue(AppUser user)
    {
        var token = JwtTokenFactory.Create(_jwt, user.Id, user.Username, user.Role);
        return new AuthResponse(token, "Bearer", _jwt.ExpiryMinutes, user.Username, user.Role);
    }
}
