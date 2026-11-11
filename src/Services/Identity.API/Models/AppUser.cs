namespace Identity.API.Models;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Customer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class Roles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}
