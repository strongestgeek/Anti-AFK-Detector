using System;
namespace AFKSentinel.Core.Models
{
    public readonly record struct MotionData
    {
        public int X { get; }
        public int Y { get; }
        public long Timestamp { get; }
        public InputType Type { get; }
        public bool IsInjected { get; } // TRUE = Software Jiggler
        public uint ExtraInfo { get; }

        public MotionData(int x, int y, InputType type, bool isInjected, uint extraInfo, long? timestamp = null)
        {
            X = x; Y = y;
            Timestamp = timestamp ?? DateTime.UtcNow.Ticks;
            Type = type;
            IsInjected = isInjected;
            ExtraInfo = extraInfo;
        }
    }
}
