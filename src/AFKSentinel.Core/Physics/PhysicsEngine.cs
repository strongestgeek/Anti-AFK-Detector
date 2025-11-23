using System;
using System.Collections.Generic;
using System.Linq;
using AFKSentinel.Core.Models;

namespace AFKSentinel.Core.Physics
{
    public enum DetectionResult
    {
        Human,
        SoftwareJiggler,
        MechanicalJiggler_Linear,
        MechanicalJiggler_Periodic,
        MechanicalJiggler_Jitter,
        MechanicalJiggler_ZeroInput
    }

    public class PhysicsEngine
    {
        private readonly List<MotionData> _motionBuffer = new List<MotionData>();

        public void AddMotionData(MotionData data)
        {
            _motionBuffer.Add(data);
        }

        public List<MotionData> GetAndClearBuffer()
        {
            var buffer = new List<MotionData>(_motionBuffer);
            _motionBuffer.Clear();
            return buffer;
        }

        private static bool IsLinear(List<MotionData> points, DetectionSettings settings)
        {
            if (points.Count < 3) return false;

            // Simple Linear Regression (y = mx + c)
            var n = (double)points.Count;
            var sumX = points.Sum(p => (double)p.X);
            var sumY = points.Sum(p => (double)p.Y);
            var sumXy = points.Sum(p => (double)p.X * p.Y);
            var sumX2 = points.Sum(p => (double)p.X * p.X);

            var slope = (n * sumXy - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            if (double.IsNaN(slope) || double.IsInfinity(slope))
            {
                // Handle vertical line case
                var firstX = points[0].X;
                return points.All(p => p.X == firstX);
            }
            
            // Calculate Mean Squared Error (MSE)
            var mse = points.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2)) / n;

            return mse < settings.LinearityThreshold;
        }

        private static bool IsPeriodic(List<MotionData> points, DetectionSettings settings)
        {
            var moveEvents = points.Where(p => p.Type == InputType.MouseMove).ToList();
            if (moveEvents.Count < 10) return false; // Need enough events for meaningful analysis

            var timeDeltas = new List<long>();
            for (int i = 1; i < moveEvents.Count; i++)
            {
                timeDeltas.Add(moveEvents[i].Timestamp - moveEvents[i - 1].Timestamp);
            }

            if (timeDeltas.Count == 0) return false;

            var avgDelta = timeDeltas.Average();
            var stdDev = Math.Sqrt(timeDeltas.Average(d => Math.Pow(d - avgDelta, 2)));

            return stdDev < settings.PeriodicityStdDevThreshold;
        }
        
        private static bool IsJittery(List<MotionData> points, DetectionSettings settings)
        {
            if (points.Count < 10) return false;

            double pathLength = 0;
            for (int i = 1; i < points.Count; i++)
            {
                pathLength += Math.Sqrt(Math.Pow(points[i].X - points[i - 1].X, 2) + Math.Pow(points[i].Y - points[i - 1].Y, 2));
            }

            var startPoint = points.First();
            var endPoint = points.Last();
            var netDisplacement = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));

            if (netDisplacement > settings.JitterDisplacementThreshold)
            {
                var ratio = pathLength / netDisplacement;
                return ratio > settings.JitterRatioThreshold; 
            }
            else if (pathLength > settings.JitterPathLengthThreshold && netDisplacement <= settings.JitterDisplacementThreshold)
            {
                return true;
            }

            return false;
        }

        private static bool IsZeroInput(List<MotionData> points, DetectionSettings settings)
        {
            if (points.Count < 2) return false;

            bool hasOtherInput = points.Any(p => p.Type != InputType.MouseMove);
            if (hasOtherInput) return false;

            double pathLength = 0;
            for (int i = 1; i < points.Count; i++)
            {
                pathLength += Math.Sqrt(Math.Pow(points[i].X - points[i - 1].X, 2) + Math.Pow(points[i].Y - points[i - 1].Y, 2));
            }

            return pathLength > settings.ZeroInputPathLengthThreshold;
        }

        public static DetectionResult Analyze(List<MotionData> points, DetectionSettings settings)
        {
            if (points == null || points.Count == 0) return DetectionResult.Human;

            if (points.Any(p => p.IsInjected))
            {
                return DetectionResult.SoftwareJiggler;
            }

            if (IsLinear(points, settings)) return DetectionResult.MechanicalJiggler_Linear;
            if (IsPeriodic(points, settings)) return DetectionResult.MechanicalJiggler_Periodic;
            if (IsJittery(points, settings)) return DetectionResult.MechanicalJiggler_Jitter;
            if (IsZeroInput(points, settings)) return DetectionResult.MechanicalJiggler_ZeroInput;

            return DetectionResult.Human;
        }
    }
}
