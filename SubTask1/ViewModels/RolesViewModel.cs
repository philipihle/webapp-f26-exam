namespace webapp_f26_exam.ViewModels;

public class UserRoleItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
}

public class RolesViewModel
{
    public List<UserRoleItemViewModel> Users { get; set; } = new();
}
