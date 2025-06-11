namespace Application.Dto_s.User;

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string? Patronymic { get; set; }
    public string? Status { get; set; }
    public DateOnly BirthDay { get; set; }
}