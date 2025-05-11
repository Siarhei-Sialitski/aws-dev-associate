namespace WebApi;

public class QueueMessageReader : BackgroundService
{
    private readonly ILogger<QueueMessageReader> _logger;
    private readonly IQueueRepository _queueRepository;

    public QueueMessageReader(ILogger<QueueMessageReader> logger, IQueueRepository queueRepository)
    {
        _logger = logger;
        _queueRepository = queueRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        // When the timer should have no due-time, then do the work once now.
        await DoWork();

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                //await DoWork();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");
        }
    }

    private async Task DoWork()
    {
        await _queueRepository.ReadMessages();
    }
}
