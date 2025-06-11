using Application.Enums;
using Application.Interfaces.Services;
using Application.Interfaces.Services.HelperServices;
using Application.Services.HelperServices.Options;
using Application.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Application.Services.HelperServices;

public class MinioService(IOptions<MinioOptions> minioOptions, ILogger<MinioService> logger) : IMinioService
{
    private readonly IMinioClient _minioClientBack = new MinioClient()
        .WithEndpoint(minioOptions.Value.EndpointBack)
        .WithCredentials(minioOptions.Value.AccessKey, minioOptions.Value.SecretKey)
        .WithSSL(false)
        .Build();
    private readonly IMinioClient _minioClientFront = new MinioClient()
        .WithEndpoint(minioOptions.Value.EndpointFront)
        .WithCredentials(minioOptions.Value.AccessKey, minioOptions.Value.SecretKey)
        .WithSSL(false)
        .Build();

    private readonly MinioOptions _minioConfig = minioOptions.Value;

    public async Task<Result> UploadFileAsync(string fileName, Stream fileStream, string contentType)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileStream == null! 
                                                  || string.IsNullOrWhiteSpace(contentType))
            return Result.Failure(new Error(ErrorType.BadRequest, 
                "Некорректные параметры для загрузки файла"));

        try
        {
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_minioConfig.BucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClientBack.PutObjectAsync(putObjectArgs);
            logger.LogInformation($"Файл {fileName} успешно загружен в Minio");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при загрузке файла {fileName} в Minio");
            return Result.Failure(new Error(ErrorType.ServerError, ex.Message));
        }
    }

    public async Task<Result<Stream>> GetFileAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Result<Stream>.Failure(new Error(ErrorType.BadRequest, 
                "Имя объекта не может быть пустым"));

        try
        {
            var memoryStream = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_minioConfig.BucketName)
                .WithObject(fileName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await _minioClientBack.GetObjectAsync(getObjectArgs);
            memoryStream.Position = 0;

            logger.LogInformation($"Файл {fileName} успешно получен из Minio");
            return Result<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при получении файла {fileName} из Minio");
            return Result<Stream>.Failure(new Error(ErrorType.ServerError, ex.Message));
        }
    }

    public async Task<Result> DeleteFileAsync(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return Result.Failure(new Error(ErrorType.BadRequest, 
                "Имя объекта не может быть пустым"));

        try
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_minioConfig.BucketName)
                .WithObject(objectName);

            await _minioClientBack.RemoveObjectAsync(removeObjectArgs);

            logger.LogInformation($"Файл {objectName} удалён из Minio");
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при удалении файла {objectName} из Minio");
            return Result.Failure(new Error(ErrorType.ServerError, ex.Message));
        }
    }

    public async Task<Result<string>> GetFileUrlAsync(string objectName, int expiryDays = 7)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return Result<string>.Failure(new Error(ErrorType.BadRequest, 
                "Имя объекта не может быть пустым"));

        try
        {
            var presignedArgs = new PresignedGetObjectArgs()
                .WithBucket(_minioConfig.BucketName)
                .WithObject(objectName)
                .WithExpiry(expiryDays * 24 * 60 * 60);

            var url = await _minioClientFront.PresignedGetObjectAsync(presignedArgs);

            logger.LogInformation($"Сгенерирована ссылка на файл {objectName}: {url}");
            return Result<string>.Success(url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Ошибка при генерации ссылки на файл {objectName}");
            return Result<string>.Failure(new Error(ErrorType.ServerError, ex.Message));
        }
    }
}