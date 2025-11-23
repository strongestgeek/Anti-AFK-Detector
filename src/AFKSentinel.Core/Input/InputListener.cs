using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AFKSentinel.Core.Models; // For MotionData and InputType

namespace AFKSentinel.Core.Input
{
    public class InputListener
    {
        // Windows API Constants
        private const int WH_MOUSE_LL = 14; // Low-level mouse hook
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;

        // P/Invoke Declarations
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Delegate for the low-level mouse hook
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                InputType type = InputType.Unknown;
                switch ((int)wParam)
                {
                    case WM_LBUTTONDOWN: type = InputType.LeftDown; break;
                    case WM_LBUTTONUP: type = InputType.LeftUp; break;
                    case WM_RBUTTONDOWN: type = InputType.RightDown; break;
                    case WM_RBUTTONUP: type = InputType.RightUp; break;
                    case WM_MBUTTONDOWN: type = InputType.MiddleDown; break;
                    case WM_MBUTTONUP: type = InputType.MiddleUp; break;
                    case WM_MOUSEMOVE: type = InputType.MouseMove; break;
                    case WM_MOUSEWHEEL: type = InputType.MouseWheel; break;
                }
                
                // Determine if event was injected (software jiggler)
                bool isInjected = (hookStruct.flags & 0x01) != 0; // LLMHF_INJECTED = 0x01

                MotionData motionData = new MotionData(
                    hookStruct.pt.x, 
                    hookStruct.pt.y, 
                    type, 
                    isInjected,
                    unchecked((uint)(long)hookStruct.dwExtraInfo),
                    hookStruct.time // Pass the timestamp from the hook struct
                );

                OnMotionDataCaptured?.Invoke(motionData);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static event Action<MotionData>? OnMotionDataCaptured; // Made nullable to fix CS8618

        public static void Start()
        {
            if (_hookID == IntPtr.Zero)
            {
                // Get the module handle of the current process
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule!) // Null-forgiving operator
                {
                    _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName!), 0); // Null-forgiving operator
                }

                if (_hookID == IntPtr.Zero)
                {
                    // Handle error: hook installation failed
                    // For now, let's just throw an exception or log it
                    throw new Exception("Failed to install mouse hook.");
                }
            }
        }

        public static void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }
}
