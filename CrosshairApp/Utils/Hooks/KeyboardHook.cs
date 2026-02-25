using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using CrosshairApp.Windows;

namespace CrosshairApp.Utils;

public class KeyboardHook
{
    private const int WhKeyboardLl = 13;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private CrosshairWindow _window;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install(CrosshairWindow window)
    {
        _window = window;
        _hookId = SetHook(_proc);
    }

    public void Uninstall()
    {
        UnhookWindowsHookEx(_hookId);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        try
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule != null && curModule.ModuleName != null)
            {
                return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
            return IntPtr.Zero;
        }
        catch (Exception)
        {
            return IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode < 0 || (wParam != (IntPtr)WmKeydown && wParam != (IntPtr)WmSyskeydown))
                return CallNextHookEx(_hookId, nCode, wParam, lParam);

            var vkCode = Marshal.ReadInt32(lParam);
            var key = KeyInterop.KeyFromVirtualKey(vkCode);

            var modifiers = Keyboard.Modifiers;
            var isCtrlShift = modifiers.HasFlag(ModifierKeys.Control) && modifiers.HasFlag(ModifierKeys.Shift);

            if (isCtrlShift && _window != null)
            {
                if (key == Key.H) _window.ToggleVisibility();
                else if (key == Key.PageUp) _window.SwitchToNextProfile();
                else if (key == Key.PageDown) _window.SwitchToPreviousProfile();
            }
        }
        catch (Exception)
        {
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
}