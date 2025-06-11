namespace Application.Services.HelperServices.Options;

public class MinioOptions
{
    public string EndpointFront { get; set; }
    public string EndpointBack { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
}