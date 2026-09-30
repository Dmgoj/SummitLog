using Amazon.S3;
using Amazon.S3.Model;

namespace SummitLog.Api.Services;

public class S3AvatarStorage(IAmazonS3 s3Client, string bucketName) : IAvatarStorage
{
    public async Task SaveAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true, // R2 doesn't support the SDK's chunked/streaming signed-payload upload mode.
            UseChunkEncoding = false
        }, cancellationToken);
    }

    public async Task<Stream?> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await s3Client.GetObjectAsync(bucketName, fileName, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await s3Client.DeleteObjectAsync(bucketName, fileName, cancellationToken);
    }
}
