using System.Text.Json.Serialization;

namespace Shelter.Models;

public class User
{
    public string id { get; set; }
    public string partitionKey { get; set; } = "user";
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Role { get; set; } = "Client";
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}