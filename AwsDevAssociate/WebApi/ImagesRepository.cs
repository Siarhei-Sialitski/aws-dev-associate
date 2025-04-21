using Amazon.S3;
using Amazon.S3.Transfer;

namespace WebApi;

public interface IImagesRepository
{
    Task UploadImage(string imageName, string base64Image);
    Task<string> DownloadImage(string imageName);
    Task DeleteImage(string imageName);
    Task<ImageMetaInfo> ImageMetainfo(string imageName);
    Task<ImageMetaInfo> ImageMetainfo();
}

public class ImagesRepository : IImagesRepository
{
    private readonly string _bucketName = "s3-awsassociate-siarhei-sialitski";
    private readonly IAmazonS3 _s3Client;

    public ImagesRepository(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task UploadImage(string imageName, string base64Image)
    {
        try
        {
            // Decode the base64 string to a byte array
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            // Create a memory stream from the byte array
            using var stream = new MemoryStream(imageBytes);

            // Use TransferUtility to upload the image to S3
            var transferUtility = new TransferUtility(_s3Client);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = imageName,
                BucketName = _bucketName,
                ContentType = "image/jpeg" // Adjust content type if needed
            };

            await transferUtility.UploadAsync(uploadRequest);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            throw new Exception("Error uploading image to S3", ex);
        }
    }

    public async Task<string> DownloadImage(string imageName)
    {
        try
        {
            // Get the object from S3
            var response = await _s3Client.GetObjectAsync(_bucketName, imageName);

            // Read the response stream into a memory stream
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);

            // Convert the memory stream to a byte array
            byte[] imageBytes = memoryStream.ToArray();

            // Convert the byte array to a base64 string
            return Convert.ToBase64String(imageBytes);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            throw new Exception("Error downloading image from S3", ex);
        }
    }

    public async Task DeleteImage(string imageName)
    {
        try
        {
            // Delete the object from S3
            await _s3Client.DeleteObjectAsync(_bucketName, imageName);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            throw new Exception("Error deleting image from S3", ex);
        }
    }

    public async Task<ImageMetaInfo> ImageMetainfo(string imageName)
    {
        try
        {
            // Get the object metadata from S3
            var response = await _s3Client.GetObjectMetadataAsync(_bucketName, imageName);
            return new ImageMetaInfo(imageName, response.ContentLength, response.LastModified, imageName.Split('.')[1]);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            throw new Exception("Error getting image metadata from S3", ex);
        }
    }

    public async Task<ImageMetaInfo> ImageMetainfo()
    {
        try
        {
            var objectKeys = new List<string>();
            var request = new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = _bucketName
            };

            Amazon.S3.Model.ListObjectsV2Response response;
            do
            {
                // Fetch the list of objects
                response = await _s3Client.ListObjectsV2Async(request);

                // Add the keys (object names) to the list
                objectKeys.AddRange(response.S3Objects.Select(o => o.Key));

                // Update the continuation token for the next request (if more objects exist)
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated); // Continue if there are more objects to fetch

            var random = new Random();
            int randomIndex = random.Next(objectKeys.Count);
            return await ImageMetainfo(objectKeys[randomIndex]);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., log the error)
            throw new Exception("Error listing objects in S3", ex);
        }
    }
}

public record ImageMetaInfo(string ImageName, long ContentLength, DateTime LastModified, string FileExtension);
