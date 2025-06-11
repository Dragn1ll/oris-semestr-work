namespace Application.Dto_s.User;

public class UserAuthInfoDto
{
    public Guid Id { get; set; }
    public string PasswordHash { get; set; }
}