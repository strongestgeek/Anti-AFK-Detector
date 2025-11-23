using AFKSentinel.Core.Input;
using AFKSentinel.Core.Models;
using AFKSentinel.Core.Physics;
using System.Timers;
using System.Diagnostics;
using Microsoft.Extensions.Options; // For IOptions

namespace AFKSentinel.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly PhysicsEngine _physicsEngine;
    private readonly DetectionSettings _detectionSettings;
    private readonly System.Timers.Timer _analysisTimer;
    private readonly Queue<MotionData> _motionBuffer;
    private const int ANALYSIS_INTERVAL_MS = 5000; // Analyze every 5 seconds
    private const int BUFFER_SIZE_LIMIT = 1000; // Limit buffer to prevent excessive memory usage

    public Worker(ILogger<Worker> logger, IOptions<DetectionSettings> detectionSettings)
    {
        _logger = logger;
        _detectionSettings = detectionSettings.Value;
        _physicsEngine = new PhysicsEngine();
        _motionBuffer = new Queue<MotionData>();

        _analysisTimer = new System.Timers.Timer(ANALYSIS_INTERVAL_MS);
        _analysisTimer.Elapsed += AnalysisTimer_Elapsed;
        _analysisTimer.AutoReset = true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial setup for InputListener
        InputListener.OnMotionDataCaptured += EnqueueMotionData;
        InputListener.Start();
        _logger.LogInformation("InputListener started.");

        _analysisTimer.Start();
        _logger.LogInformation("Analysis timer started.");

        // Keep the service running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _analysisTimer.Stop();
        _logger.LogInformation("Analysis timer stopped.");

        InputListener.Stop();
        InputListener.OnMotionDataCaptured -= EnqueueMotionData;
        _logger.LogInformation("InputListener stopped.");

        return base.StopAsync(cancellationToken);
    }

    private void EnqueueMotionData(MotionData data)
    {
        lock (_motionBuffer)
        {
            _motionBuffer.Enqueue(data);
            // Optionally, trim the buffer if it gets too large
            while (_motionBuffer.Count > BUFFER_SIZE_LIMIT)
            {
                _motionBuffer.Dequeue();
            }
        }
    }

    private void AnalysisTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        List<MotionData> currentBuffer;
        lock (_motionBuffer)
        {
            currentBuffer = _motionBuffer.ToList(); // Take a snapshot for analysis
            _motionBuffer.Clear(); // Clear buffer after snapshot
        }

        if (currentBuffer.Count == 0)
        {
            _logger.LogDebug("No motion data to analyze.");
            return;
        }

        DetectionResult result = PhysicsEngine.Analyze(currentBuffer, _detectionSettings);
        if (result != DetectionResult.Human)
        {
            string message = $"AFK-Sentinel Detection: {result}. Motion events analyzed: {currentBuffer.Count}";
            _logger.LogWarning(message); // Log to general logger
            EventLogSource.WriteEntry(message, System.Diagnostics.EventLogEntryType.Warning, EventLogSource.EVENT_ID_DETECTION); // Log to Windows Event Log
        }
        else
        {
            _logger.LogInformation("AFK-Sentinel: Human activity detected. Motion events analyzed: {count}", currentBuffer.Count);
        }
    }
}
