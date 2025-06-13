namespace HabitHub.Endpoints;

public static class PagesEndpoints
{
    public static void AddPages(this IEndpointRouteBuilder app)
    {
        app.MapGet("/login", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/pages/auth/login.html");
        });
        
        app.MapGet("/register", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/pages/auth/register.html");
        });
        
        app.MapGet("/main", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/pages/main/main.html");
        });
        
        app.MapGet("/habits", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/pages/habit/habit.html");
        });
        
        app.MapGet("/chats", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/pages/chat/chat.html");
        });
        
        app.MapGet("/profile/{userId:guid}", async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync("wwwroot/pages/profile/profile.html");
        });
    }
}