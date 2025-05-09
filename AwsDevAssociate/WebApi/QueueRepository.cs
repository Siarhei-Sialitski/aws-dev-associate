using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace WebApi;

public interface IQueueRepository
{
    Task SendMessage(ImageMetaInfo imageMetaInfo);
    Task ReadMessages();
}

public class QueueRepository : IQueueRepository
{
    //private string sqsUrl = "https://sqs.us-east-1.amazonaws.com/182399713898/sqs-aws-associate-ss-uploads-notification-queue";
    private readonly IAmazonSQS _sqsClient;
    private readonly ISnsRepository _snsRepository;

    public QueueRepository(IAmazonSQS sqsClient, ISnsRepository snsRepository)
    {
        _sqsClient = sqsClient;
        _snsRepository = snsRepository;
    }

    public async Task SendMessage(ImageMetaInfo imageMetaInfo)
    {
        var sqsUrl = await _sqsClient.GetQueueUrlAsync("sqs-aws-associate-ss-uploads-notification-queue");
        var response = await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = sqsUrl.QueueUrl,
            MessageBody = JsonSerializer.Serialize(imageMetaInfo)
        });
    }

    public async Task ReadMessages()
    {
        var sqsUrl = await _sqsClient.GetQueueUrlAsync("sqs-aws-associate-ss-uploads-notification-queue");
        var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = sqsUrl.QueueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 5
        });

        if (response.Messages == null)
        {
            return;
        }

        foreach (var message in response.Messages)
        {
            // Process the message
            Console.WriteLine($"Received message: {message.Body}");
            var imageMetaInfo = JsonSerializer.Deserialize<ImageMetaInfo>(message.Body);
            await _snsRepository.PublishMessage(imageMetaInfo);

            // Delete the message after processing
            await _sqsClient.DeleteMessageAsync(sqsUrl.QueueUrl, message.ReceiptHandle);
        }
    }
}
