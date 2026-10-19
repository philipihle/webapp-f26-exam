namespace webapp_f26_exam.ViewModels;

public class UserRoleItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
}

public class RolesViewModel
{
    public int TotalRoles => 2;
    public int AdminCount { get; set; }
    public int StudentCount { get; set; }
    public int TotalUsers { get; set; }
    public List<UserRoleItemViewModel> Users { get; set; } = new();
}
