using Application.Utils;

namespace Application.Interfaces.Services.HelperServices;

public interface IMinioService
{
    Task<Result> UploadFileAsync(string fileName, Stream fileStream, string contentType);
    Task<Result<Stream>> GetFileAsync(string fileName);
    Task<Result> DeleteFileAsync(string objectName);
    Task<Result<string>> GetFileUrlAsync(string objectName, int expiryDays = 7);
}