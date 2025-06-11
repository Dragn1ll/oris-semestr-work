namespace HabitHub.Requests.Auth;

public record RegisterRequest(
    string Name,
    string Surname,
    string? Patronymic,
    string Email,
    string Password,
    string? Status,
    DateOnly BirthDay
    );