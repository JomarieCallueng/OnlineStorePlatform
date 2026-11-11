using System.ComponentModel.DataAnnotations;

namespace Identity.API.Models;

public record RegisterRequest([Required] string Username, [Required] string Password);

public record LoginRequest([Required] string Username, [Required] string Password);

public record AuthResponse(string AccessToken, string TokenType, int ExpiresInMinutes, string Username, string Role);
