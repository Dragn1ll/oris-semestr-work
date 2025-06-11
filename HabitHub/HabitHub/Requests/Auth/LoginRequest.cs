namespace HabitHub.Requests.Auth;

public record LoginRequest(
    string Email,
    string Password
    );