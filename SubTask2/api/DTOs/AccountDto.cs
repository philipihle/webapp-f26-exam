using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class AccountDto
{
    public string? UserName { get; set; }
    [Required]
    public string? Email { get; set; }
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? Role { get; set; }

}