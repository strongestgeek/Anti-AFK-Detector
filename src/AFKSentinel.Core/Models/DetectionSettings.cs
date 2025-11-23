namespace AFKSentinel.Core.Models
{
    public class DetectionSettings
    {
        public double LinearityThreshold { get; set; } = 0.5;
        public double PeriodicityStdDevThreshold { get; set; } = 10.0;
        public double JitterRatioThreshold { get; set; } = 5.0;
        public int JitterDisplacementThreshold { get; set; } = 5;
        public int JitterPathLengthThreshold { get; set; } = 20;
        public int ZeroInputPathLengthThreshold { get; set; } = 100;
    }
}
