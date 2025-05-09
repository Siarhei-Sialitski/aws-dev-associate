using System.Text;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace WebApi;

public interface ISnsRepository
{
    Task AddSubscription(string email);
    Task RemoveSubscription(string email);
    Task PublishMessage(ImageMetaInfo imageMetaInfo);
}

public class SnsRepository : ISnsRepository
{
    private readonly IAmazonSimpleNotificationService _client;

    public SnsRepository(IAmazonSimpleNotificationService client)
    {
        _client = client;
    }

    public async Task AddSubscription(string email)
    {
        var topics = await _client.ListTopicsAsync();
        var topicArn = topics.Topics.FirstOrDefault(x => x.TopicArn.Contains("sns-aws-associate-ss-uploads-notification-topic"))?.TopicArn;

        if (topicArn == null)
        {
            throw new Exception("Topic not found");
        }

        var request = new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = "email",
            Endpoint = email
        };

        var response = await _client.SubscribeAsync(request);
    }

    public async Task RemoveSubscription(string email)
    {
        var topics = await _client.ListTopicsAsync();
        var topicArn = topics.Topics.FirstOrDefault(x => x.TopicArn.Contains("sns-aws-associate-ss-uploads-notification-topic"))?.TopicArn;

        if (topicArn == null)
        {
            throw new Exception("Topic not found");
        }

        var subscriptions = await _client.ListSubscriptionsByTopicAsync(topicArn);
        var subscription = subscriptions.Subscriptions.FirstOrDefault(x => x.Endpoint == email);

        if (subscription == null) {
            throw new Exception("Subscription not found");
        }

        if (subscription.SubscriptionArn == "PendingConfirmation")
        {
            Console.WriteLine("Subscription is not confirmed");
            return;
        }

        await _client.UnsubscribeAsync(subscription.SubscriptionArn);
    }

    public async Task PublishMessage(ImageMetaInfo imageMetaInfo)
    {
        var topics = await _client.ListTopicsAsync();
        var topicArn = topics.Topics.FirstOrDefault(x => x.TopicArn.Contains("sns-aws-associate-ss-uploads-notification-topic"))?.TopicArn;

        if (topicArn == null)
        {
            throw new Exception("Topic not found");
        }

        var stringBuidler = new StringBuilder();

        stringBuidler.AppendLine("The image was uploaded successfully.");
        stringBuidler.AppendLine($"Image Name: {imageMetaInfo.ImageName}");
        stringBuidler.AppendLine($"Image Name: {imageMetaInfo.FileExtension}");
        stringBuidler.AppendLine($"Image Size: {imageMetaInfo.ContentLength}");
        stringBuidler.AppendLine($"Image Last Modified: {imageMetaInfo.LastModified}");
        stringBuidler.AppendLine($"Image URL: https://s3-awsassociate-siarhei-sialitski.s3.amazonaws.com/{imageMetaInfo.ImageName}");

        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = stringBuidler.ToString()
        };

        var response = await _client.PublishAsync(request);
    }
}
