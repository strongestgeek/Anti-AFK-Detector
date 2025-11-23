using System.Runtime.InteropServices;
namespace AFKSentinel.Core.Input
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags; // Contains LLMHF_INJECTED
        public uint time;
        public System.IntPtr dwExtraInfo; 
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { public int x; public int y; }
}
