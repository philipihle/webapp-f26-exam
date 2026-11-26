using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using api.DTOs;

namespace api.Controllers;

// Register and login. Login returns a JWT that the frontend sends with protected requests.
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    //HTTP POST endpoint to register a new user. Returns 400 with the Identity errors if it fails (e.g. weak password).
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var user = new IdentityUser
        {
            UserName = registerDto.Username,
            Email = registerDto.Email
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("[AuthController] user registered for {@username}", registerDto.Username);
            return Ok(new { Message = "User registered successfully" });
        }

        _logger.LogWarning("[AuthController] user registration failed for {@username}", registerDto.Username);
        return BadRequest(result.Errors);
    }

    //HTTP POST endpoint to log in. Returns a JWT if the username and password are correct, otherwise 401.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _userManager.FindByNameAsync(loginDto.Username);

        if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            _logger.LogInformation("[AuthController] user authorised for {@username}", loginDto.Username);
            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        _logger.LogWarning("[AuthController] user not authorised for {@username}", loginDto.Username);
        return Unauthorized();
    }

    // Creates a signed JWT with the user's name, email and id, valid for 120 minutes
    private string GenerateJwtToken(IdentityUser user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            _logger.LogError("[AuthController] JWT key is missing from configuration.");
            throw new InvalidOperationException("JWT key is missing from configuration.");
        }
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName!), // subject: the username
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id), // user id, used later for bookings
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // unique token id
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // issued at
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        _logger.LogInformation("[AuthController] JWT token created for {@username}", user.UserName);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
