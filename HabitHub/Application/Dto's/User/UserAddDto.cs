namespace Application.Dto_s.User;

public class UserAddDto
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string? Patronymic { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? Status { get; set; }
    public DateOnly BirthDay { get; set; }
}