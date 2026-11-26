using System.ComponentModel.DataAnnotations;

namespace webapp_f26_exam.ViewModels;

public class UserEditViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;
}

