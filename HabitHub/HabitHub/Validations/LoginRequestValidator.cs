using FluentValidation;
using HabitHub.Requests.Auth;

namespace HabitHub.Validations;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный email");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов");
    }
}