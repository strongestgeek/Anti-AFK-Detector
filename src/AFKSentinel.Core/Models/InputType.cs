namespace AFKSentinel.Core.Models
{
    public enum InputType : byte
    {
        Unknown = 0,
        MouseMove = 1,
        LeftDown = 2, LeftUp = 3,
        RightDown = 4, RightUp = 5,
        MiddleDown = 6, MiddleUp = 7,
        MouseWheel = 8
    }
}
