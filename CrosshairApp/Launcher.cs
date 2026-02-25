using System;
using System.IO;
using System.Reflection;
using CrosshairApp.Utils;
using CrosshairApp.Windows;
using Application = System.Windows.Application;

namespace CrosshairApp;

public static class Launcher
{
    public static string ConfigFilePath;
    private static KeyboardHook _keyboardHook;
    public const string Version = "1.1.0";

    [STAThread]
    public static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => { };

        var app = new Application();
        app.DispatcherUnhandledException += (sender, args) =>
        {
            args.Handled = true;
        };

        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath);
        var baseDir = string.IsNullOrWhiteSpace(assemblyDir) ? AppDomain.CurrentDomain.BaseDirectory : assemblyDir;

        if (string.IsNullOrWhiteSpace(baseDir)) return;

        ConfigFilePath = Path.Combine(baseDir, "config.properties");

        if (!File.Exists(ConfigFilePath)) ConfigUtils.CreateConfig();

        var window = new CrosshairWindow();

        _keyboardHook = new KeyboardHook();
        _keyboardHook.Install(window);

        app.Run(window);

        app.Exit += (sender, args) => _keyboardHook.Uninstall();
    }
}