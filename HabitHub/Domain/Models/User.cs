namespace Domain.Models;

public class User(
    Guid id,
    string name,
    string surname,
    string? patronymic,
    string email,
    string passwordHash,
    string? status,
    DateOnly birthday
    )
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string Surname { get; } = surname;
    public string? Patronymic { get; } = patronymic;
    public string Email { get; } = email;
    public string PasswordHash { get; } = passwordHash;
    public string? Status { get; } = status;
    public DateOnly BirthDay { get; } = birthday;

    public static User Create(Guid id, string name, string surname, string? patronymic, 
        string email, string passwordHash, string? status, DateOnly birthday)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User id cannot be empty");
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name cannot be empty");
        
        if (string.IsNullOrWhiteSpace(surname))
            throw new ArgumentException("User surname cannot be empty");
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("User email cannot be empty");
        
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("User password hash cannot be empty");
        
        if (birthday == default)
            throw new ArgumentException("User birthday cannot be empty");
        
        if (!IsAdult(birthday))
            throw new ArgumentException("User is not adult");

        return new User(id, name, surname, patronymic, email, passwordHash, status, birthday);
    }
    
    private static bool IsAdult(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Year;
        
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }
    
        return age >= 16;
    }
}