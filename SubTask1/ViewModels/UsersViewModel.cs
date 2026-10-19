namespace webapp_f26_exam.ViewModels;

public class UsersViewModel{
    public IEnumerable<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();
}

public class UserItemViewModel{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
