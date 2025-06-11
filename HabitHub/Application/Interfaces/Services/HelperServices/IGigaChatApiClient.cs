using Application.Utils;

namespace Application.Interfaces.Services.HelperServices;

public interface IGigaChatApiClient
{
    Task<Result<string>> GetAccessTokenAsync();
    Task<Result<string>> SendMessageAsync(string accessToken, string message);
}