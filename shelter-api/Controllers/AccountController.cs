using Microsoft.AspNetCore.Mvc;
using Shelter.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Shelter.Services;
using Shelter.Services.KdfService;

namespace Shelter.Controllers;

[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
{
    private readonly string _jwtSecret;
    private readonly CosmosDbService _cosmosDbService;
    private readonly IKdfService _kdfService;

    public AccountController(CosmosDbService cosmosDbService, IKdfService kdfService)
    {
        _cosmosDbService = cosmosDbService;
        _kdfService = kdfService;
        _jwtSecret = "eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTcxMjAwMTQyOSwiaWF0IjoxNzEyMDAxNDI5fQ.DjkDOb1Cize0xASN86musY6r7tkNrNMVY8rmjYKIAo4\n";
    }

    [HttpPost]
    [Route("RegisterUser")]
    public async Task<IActionResult> RegisterUser([FromBody] UserRegistrationRequest model)
    {
        if (model == null)
        {
            return BadRequest("Invalid user data received");
        }

        if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.Role))
        {
            return BadRequest("All fields are required");
        }

        if (model.Password.Length < 6)
        {
            return BadRequest("Password must be at least 6 characters long");
        }

        // Check if user already exists
        var users = await _cosmosDbService.GetItemsAsync<User>("SELECT * FROM c WHERE c.email = @email", ("@email", model.Email));
        var existingUser = users.FirstOrDefault();
        if (existingUser!= null)
        {
            return Conflict("User with this email already exists");
        }

        // Hash password with salt
        var salt = _kdfService.GenerateSalt();
        var passwordHash = _kdfService.GetDerivedKey(model.Password, salt);

        // Create new user
        var user = new User
        {
            id = Guid.NewGuid().ToString(),
            partitionKey = "user",
            Name = model.Name,
            Email = model.Email,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            Role = model.Role,
            Phone = null,
            Address = null,
            City = null,
            Token = null,
            Avatar = null
        };

        // Save user to database
        await _cosmosDbService.AddItemAsync(user);

        return Ok();
    }

    [HttpPatch]
    [Route("EditUser/{id}")]
    public async Task<IActionResult> EditUser(string id, [FromBody]User user)
    {
        if (id!= user.id)
        {
            return BadRequest("Invalid user data received. User id does not match the id in the URL");
        }

        var existingUser = (await _cosmosDbService.GetItemsAsync<User>($"SELECT * FROM c WHERE c.id = \"{id}\"")).FirstOrDefault();
        if (existingUser == null)
        {
            return NotFound("User not found");
        }

        existingUser.Login = user.Login?? existingUser.Login;
        existingUser.Name = user.Name?? existingUser.Name;
        existingUser.Email = user.Email?? existingUser.Email;
        existingUser.Phone = user.Phone?? existingUser.Phone;
        existingUser.Address = user.Address?? existingUser.Address;
        existingUser.City = user.City?? existingUser.City;
        existingUser.Role = user.Role?? existingUser.Role;
        existingUser.Avatar = user.Avatar?? existingUser.Avatar;

        await _cosmosDbService.UpdateItemAsync(id, existingUser);

        return Ok("User updated successfully");
    }

    [HttpPost]
    [Route("UploadImage/{id}")]
    public async Task<IActionResult> UploadAvatar(string id, [FromForm] IFormFile file)
    {
        if (file == null)
        {
            return BadRequest(new { message = "No file received" });
        }

        var user = (await _cosmosDbService.GetItemsAsync<User>($"SELECT * FROM c WHERE c.id = \"{id}\"")).FirstOrDefault();
        if (user == null)
        {
            return NotFound("User not found");
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "Empty file received" });
        }

        if (file.Length > 1048576)
        {
            return BadRequest(new { message = "File size exceeds 1MB" });
        }

        var extension = Path.GetExtension(file.FileName);
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
        {
            return BadRequest(new { message = "Invalid file type. Only .jpg, .jpeg and .png are allowed" });
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        // Directory.GetCurrentDirectory(), "wwwroot", "wwwroot", "avatars"
        var filePath = Path.Combine("D:\\home",  fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.Avatar = fileName;
        await _cosmosDbService.UpdateItemAsync(id, user);

        return Ok(new { message = "Avatar uploaded successfully" });
    }

    [HttpGet]
    [Route("GetImageByFilename")]
    public async Task<IActionResult> GetImageByFilename(string filename)
    {
        var filepath = Path.Combine("D:\\home", filename);
        if (!System.IO.File.Exists(filepath))
        {
            return NotFound("Image not found");
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filepath, out var mimeType))
        {
            mimeType = "application/octet-stream";
        }

        return File(System.IO.File.ReadAllBytes(filepath), mimeType);
    }


    [HttpGet]
    [Route("GetUserById/{id}")]
    public async Task<IActionResult> GetUserById(String id)
    {
        var user = (await _cosmosDbService.GetItemsAsync<User>($"SELECT * FROM c WHERE c.id = \"{id}\"")).FirstOrDefault();
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
    [HttpPost]
    [Route("AuthenticateUser")]
    public IActionResult AuthenticateUser(String userEmail, String userPassword)
    {

        // Check if the userLogin and userPassword not empty
        if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userPassword))
        {
            return BadRequest("Invalid user data received");
        }

        // Get the user from the database
        var users = (_cosmosDbService.GetItemsAsync<User>($"SELECT * FROM c WHERE c.partitionKey = \"user\"")).Result;
        var user = users.FirstOrDefault(u => u.Email == userEmail);

        // Check if the user exists
        if (user == null)
        {
            return NotFound();
        }

        // Check if the userLogin and userPassword are correct
        var passwordHash = _kdfService.GetDerivedKey(userPassword, user.PasswordSalt);
        if (userEmail == user.Email && passwordHash == user.PasswordHash)
        {
            // Generate a token for the user
            var token = GenerateJwtToken(user.id);
            user.Token = token;
            // Return the token and the user
            var response = new { token, user = new
                {
                    user.id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.Phone,
                    user.Address,
                    user.City,
                    user.Avatar,
                    user.Token
                }
            };
            return Ok(response);
        }
        return BadRequest();
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