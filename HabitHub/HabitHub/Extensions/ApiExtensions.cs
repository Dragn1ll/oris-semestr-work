using System.Text;
using Application.Interfaces.Auth;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.HelperServices;
using Application.Interfaces.Services.MainServices;
using Application.Services.HelperServices;
using Application.Services.HelperServices.Options;
using Application.Services.MainServices;
using HabitHub.Profiles;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Persistence.DataAccess;
using Persistence.DataAccess.Repositories;
using Quartz;
using StackExchange.Redis;
using Telegram.Bot;

namespace HabitHub.Extensions;

public static class ApiExtensions
{
    public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));

        var jwtOptions = configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions!.SecretKey))
                };
            });

        services.AddAuthorization();

        services.AddSingleton<IJwtWorker, JwtWorker>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
    }

    public static void AddGoogle(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = configuration.GetValue<string>("Redis:ConnectionString");
            return ConnectionMultiplexer.Connect(config!);
        });
        
        services.Configure<GoogleOptions>(configuration.GetSection("Google"));
        
        services.AddSingleton<IGoogleTokenStore, RedisGoogleTokenStore>();
        services.AddSingleton<IGoogleFitService, GoogleFitService>();
        services.AddHttpClient();
    }

    public static void AddGigaChat(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GigaChatOptions>(configuration.GetSection("GigaChat"));
        
        services.AddSingleton<IGigaChatApiClient, GigaChatApiClient>();
        services.AddSingleton<IAiService, GigaChatAiService>();
    }

    public static void AddMinio(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MinioOptions>(configuration.GetSection("Minio"));
        services.AddSingleton<IMinioService, MinioService>();
    }

    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IGoogleService, GoogleService>();
        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
    }

    public static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => 
        {
            options.UseNpgsql(configuration.GetConnectionString(nameof(AppDbContext)));
        });

        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHabitProgressRepository, HabitProgressRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }

    public static void AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITelegramChatStore, RedisTelegramChatStore>();
        services.AddScoped<TelegramUpdateHandler>();
        services.AddHostedService<TelegramBotHostedService>();
        
        services.AddSingleton<TelegramBotClient>(_ => 
            new TelegramBotClient(configuration["TelegramBotToken"]!));
        
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            
            var jobKey = new JobKey("daily-notifications");
            q.AddJob<NotificationJob>(opts => opts.WithIdentity(jobKey));
            
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("daily-notifications-trigger")
                .WithSchedule(CronScheduleBuilder
                    .DailyAtHourAndMinute(6, 0)
                    .InTimeZone(TimeZoneInfo.Utc)));
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}