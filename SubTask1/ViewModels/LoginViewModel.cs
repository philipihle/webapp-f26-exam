using System.ComponentModel.DataAnnotations;

namespace webapp_f26_exam.ViewModels;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

}
