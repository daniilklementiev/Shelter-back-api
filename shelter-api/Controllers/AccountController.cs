using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shelter.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Shelter.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly string _jwtSecret;

    public AccountController()
    {
        _jwtSecret = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTcxMjAwMTQyOSwiaWF0IjoxNzEyMDAxNDI5fQ.DjkDOb1Cize0xASN86musY6r7tkNrNMVY8rmjYKIAo4\n";
    }

    [HttpPost]
    [Route("RegisterUser")]
    public IActionResult RegisterUser([FromBody] User model)
    {
        if (model == null)
        {
            return BadRequest("Invalid user data received");
        }

        model.id = Guid.NewGuid(); // generating a new GUID for the user
        var token = GenerateJwtToken(model.username); // creating a token for the user

        var response = new { token, model.id, model.username, model.password }; // creating a response object
        return CreatedAtAction(nameof(RegisterUser), response);
    }

    [HttpPost]
    [Route("AuthenticateUser")]
    public IActionResult AuthenticateUser([FromBody] User model)
    {
        if (model == null)
        {
            return BadRequest("Invalid user data received");
        }

        return Ok($"User '{model.username}' authenticated successfully. Password '{model.password}' was checked in plain text.");
    }

    private string GenerateJwtToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler(); // creating a token handler
        var key = Encoding.ASCII.GetBytes(_jwtSecret); // encoding the secret key
        var tokenDescriptor = new SecurityTokenDescriptor // creating a token descriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor); // creating a token
        return tokenHandler.WriteToken(token);
    }
}