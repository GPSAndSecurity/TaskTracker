using Amazon.S3;
using Amazon.S3.Model;

public class S3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3Service(IAmazonS3 s3Client, IConfiguration config)
    {
        _s3Client = s3Client;

        // INTENTA LEER DESDE ENV (.env)
        _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET");

        // SI NO EXISTE EN .ENV, LO TOMA DESDE appsettings.json
        if (string.IsNullOrEmpty(_bucketName))
            _bucketName = config["AWS:BucketName"];

        if (string.IsNullOrEmpty(_bucketName))
            throw new Exception("Bucket name not found in .env or appsettings.json");
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string key, string contentType)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("File stream is empty");

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = true
        };

        await _s3Client.PutObjectAsync(putRequest);

        // URL PÚBLICA DEL ARCHIVO
        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }
}
