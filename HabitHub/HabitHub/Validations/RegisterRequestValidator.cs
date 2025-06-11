using FluentValidation;
using HabitHub.Requests.Auth;

namespace HabitHub.Validations;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно");
        
        RuleFor(x => x.Surname)
            .NotEmpty().WithMessage("Фамилия обязательна");

        RuleFor(x => x.BirthDay)
            .NotEmpty().WithMessage("Дата рождения обязательна");
    }
}