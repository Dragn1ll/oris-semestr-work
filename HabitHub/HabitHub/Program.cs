using HabitHub.Controllers;
using HabitHub.Endpoints;
using HabitHub.Extensions;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Persistence.DataAccess;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

configuration.AddEnvironmentVariables();

var services = builder.Services;

services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5000", 
                "https://localhost:5000"
            )
            .WithOrigins(
                "https://accounts.google.com",
                "https://oauth2.googleapis.com",
                "https://www.googleapis.com",
                "https://securetoken.googleapis.com",
                "https://fitness.googleapis.com"
            )
            .WithOrigins(
                "https://ngw.devices.sberbank.ru",
                "https://gigachat.devices.sberbank.ru",
                "https://gigachat.devices.sberbank.ru:9443"
            )
            .WithOrigins(
                "http://localhost:9000",
                "https://localhost:9000",
                "http://minio:9000",
                "https://minio:9000"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

services.AddOpenApi();
services.AddAuthentication();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HabitHub API",
        Version = "v1"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Введите 'Bearer' и затем ваш токен.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

services.AddAuthentication(configuration);
services.AddGigaChat(configuration);
services.AddGoogle(configuration);
services.AddMinio(configuration);
services.AddServices();
services.AddDatabase(configuration);
services.AddTelegramBot(configuration);
services.AddControllers();
services.AddSignalR();

services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapUserEndpoints();
app.MapHabitEndpoints();
app.MapAuthEndpoints();
app.MapControllers();
app.MapGoogleEndpoints();
app.MapMessageEndpoints();
app.AddPages();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict,
    Secure = CookieSecurePolicy.Always,
    HttpOnly = HttpOnlyPolicy.Always
});
        
using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    try
    {
        var context = service.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = service.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при применении миграций.");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(swu =>
{
    swu.SwaggerEndpoint("/swagger/v1/swagger.json", "HabitHub API V1");
    swu.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<ChatHub>("/chatHub");

app.Run();