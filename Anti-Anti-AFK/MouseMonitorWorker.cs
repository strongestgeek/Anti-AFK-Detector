using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace MouseMonitorService
{
    public class MouseMonitorWorker : BackgroundService
    {
        private readonly ILogger<MouseMonitorWorker> _logger;
        private Point? _lastPosition;
        private readonly ConcurrentQueue<MouseMovement> _movements;
        private const int PERIODIC_THRESHOLD_MS = 30000; // 30 seconds
        private const int PERIODIC_TOLERANCE_MS = 500;   // 0.5 seconds
        private const int CONTINUOUS_THRESHOLD_MS = 300000; // 5 minutes
        private const double MIN_MOVEMENT_DISTANCE = 5.0;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        public MouseMonitorWorker(ILogger<MouseMonitorWorker> logger)
        {
            _logger = logger;
            _movements = new ConcurrentQueue<MouseMovement>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    MonitorMouseMovement();
                    await Task.Delay(100, stoppingToken); // Check every 100ms
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mouse monitoring service");
            }
        }

        private void MonitorMouseMovement()
        {
            if (GetCursorPos(out Point currentPosition))
            {
                var timestamp = DateTimeOffset.UtcNow;

                if (_lastPosition.HasValue)
                {
                    var distance = CalculateDistance(_lastPosition.Value, currentPosition);

                    if (distance >= MIN_MOVEMENT_DISTANCE)
                    {
                        var movement = new MouseMovement
                        {
                            Position = currentPosition,
                            Timestamp = timestamp,
                            Distance = distance
                        };

                        _movements.Enqueue(movement);
                        CheckPatterns();

                        // Keep queue size manageable
                        while (_movements.Count > 1000)
                        {
                            _movements.TryDequeue(out _);
                        }
                    }
                }

                _lastPosition = currentPosition;
            }
        }

        private void CheckPatterns()
        {
            CheckPeriodicMovements();
            CheckContinuousMovement();
        }

        private void CheckPeriodicMovements()
        {
            var movements = _movements.ToArray();
            if (movements.Length < 3) return;

            var last3 = movements.TakeLast(3).ToArray();
            var intervals = new[]
            {
                (last3[1].Timestamp - last3[0].Timestamp).TotalMilliseconds,
                (last3[2].Timestamp - last3[1].Timestamp).TotalMilliseconds
            };

            if (intervals.All(i => Math.Abs(i - PERIODIC_THRESHOLD_MS) < PERIODIC_TOLERANCE_MS))
            {
                _logger.LogWarning("Suspicious periodic movement detected: {Intervals}ms", 
                    string.Join(", ", intervals));
            }
        }

        private void CheckContinuousMovement()
        {
            var movements = _movements.ToArray();
            if (movements.Length < 2) return;

            var recentMoves = movements.TakeLast(20).ToArray();
            var duration = (recentMoves.Last().Timestamp - recentMoves.First().Timestamp).TotalMilliseconds;

            if (duration >= CONTINUOUS_THRESHOLD_MS)
            {
                _logger.LogWarning("Suspicious continuous movement detected: Duration={Duration}ms", 
                    duration);
            }
        }

        private double CalculateDistance(Point p1, Point p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public class MouseMovement
    {
        public Point Position { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public double Distance { get; set; }
    }
}