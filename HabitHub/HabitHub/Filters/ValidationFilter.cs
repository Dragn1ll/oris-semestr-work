using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HabitHub.Filters;

public class ValidationFilter<TRequest> : IEndpointFilter 
    where TRequest : class
{

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        if (validator is null)
            return await next(context);

        var argument = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (argument is null)
            return Results.Problem("Неверный тип запроса", statusCode:400);

        var result = await validator.ValidateAsync(argument);
        
        if (result.IsValid) return await next(context);
        
        var errors = result.ToDictionary();
        return Results.ValidationProblem(errors);
    }
}