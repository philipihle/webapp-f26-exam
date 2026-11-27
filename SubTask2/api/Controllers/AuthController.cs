using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using api.DTOs;
using Microsoft.AspNetCore.Authorization;

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
            await _userManager.AddToRoleAsync(user, "Student"); // new users are students, as in SubTask1
            _logger.LogInformation("[AuthController] user registered for {@username}", registerDto.Username);
            return Ok(new { Message = "User registered successfully" });
        }

        _logger.LogWarning("[AuthController] user registration failed for {@username}", registerDto.Username);
        return BadRequest(result.Errors);
    }

    //HTTP POST endpoint to log in with email and password. Returns a JWT if they are correct, otherwise 401.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);

        if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            _logger.LogInformation("[AuthController] user authorised for {@email}", loginDto.Email);
            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        _logger.LogWarning("[AuthController] user not authorised for {@email}", loginDto.Email);
        return Unauthorized();
    }

    [Authorize]
    [HttpPut("update")]
    public async Task<IActionResult> UpdateAccount([FromBody] AccountDto accountDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;
        if (user == null)
        {
            _logger.LogWarning("[AuthController] user not found for {@email}", accountDto.Email);
            return NotFound("User not found");
        }

        if (!string.Equals(user.Email, accountDto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userManager.FindByEmailAsync(accountDto.Email!);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return BadRequest(new { title = "Email is already in use." });
            }

            var emailResult = await _userManager.SetEmailAsync(user, accountDto.Email);
            if (!emailResult.Succeeded)
            {
                return BadRequest(emailResult.Errors);
            }
        }

        if (!string.IsNullOrEmpty(accountDto.Password))
        {
            if (accountDto.Password != accountDto.ConfirmPassword)
            {
                _logger.LogWarning("[AuthController] password and confirm password do not match for {@email}", accountDto.Email);
                return BadRequest("Password and Confirm Password do not match");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, accountDto.Password);

            if (!resetResult.Succeeded)
            {
                _logger.LogWarning("[AuthController] password reset failed for {@email}", accountDto.Email);
                return BadRequest(resetResult.Errors);
            }
        }

        _logger.LogInformation("[AuthController] user updated for {@email}", accountDto.Email);
        return Ok(new { Message = "User updated successfully" });
    }

    // Creates a signed JWT with the user's name, email, id and role, valid for 120 minutes
    private async Task<string> GenerateJwtToken(IdentityUser user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            _logger.LogError("[AuthController] JWT key is missing from configuration.");
            throw new InvalidOperationException("JWT key is missing from configuration.");
        }
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName!), // subject: the username
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id), // unique identifier for the user
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // unique token id
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // issued at
        };

        // Add the user's roles (Admin or Student) so [Authorize(Roles = "Admin")] and the frontend can use them
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

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
