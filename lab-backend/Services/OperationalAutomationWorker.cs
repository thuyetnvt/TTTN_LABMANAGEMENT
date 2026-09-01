namespace LabManagementAPI.Services;

public sealed class OperationalAutomationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OperationalAutomationWorker> _logger;

    public OperationalAutomationWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OperationalAutomationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Automation:Enabled", true))
        {
            _logger.LogInformation("Operational automation is disabled.");
            return;
        }

        var pollMinutes = Math.Clamp(_configuration.GetValue("Automation:PollMinutes", 5), 1, 1440);
        await RunIterationAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(pollMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunIterationAsync(stoppingToken);
        }
    }

    private async Task RunIterationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<OperationalAutomationRunner>();
            await runner.RunOnceAsync(DateTime.UtcNow, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Operational automation iteration failed.");
        }
    }
}
