using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CrosshairApp.Utils
{
    public abstract class MouseHook
    {
        private const int WhMouseLl = 14;
        private const int WmRbuttondown = 0x0204;
        private const int WmRbuttonup = 0x0205;

        private static LowLevelMouseProc _proc;
        private static IntPtr _hookId = IntPtr.Zero;

        public static event EventHandler RightMouseDown;
        public static event EventHandler RightMouseUp;

        public static void Start()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        public static void Stop()
        {
            UnhookWindowsHookEx(_hookId);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            try
            {
                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                if (curModule != null && curModule.ModuleName != null)
                {
                    return SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
                return IntPtr.Zero;
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode < 0) return CallNextHookEx(_hookId, nCode, wParam, lParam);

                if (wParam == (IntPtr)WmRbuttondown)
                {
                    RightMouseDown?.Invoke(null, EventArgs.Empty);
                }
                else if (wParam == (IntPtr)WmRbuttonup)
                {
                    RightMouseUp?.Invoke(null, EventArgs.Empty);
                }
            }
            catch (Exception)
            {
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}