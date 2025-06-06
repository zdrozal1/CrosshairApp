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
    public const string Version = "1.0.0";

    [STAThread]
    public static void Main()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath);

        ConfigFilePath = Path.Combine(assemblyDir ?? throw new InvalidOperationException(), "config.properties");

        if (!File.Exists(ConfigFilePath)) ConfigUtils.CreateConfig();

        var app = new Application();
        var window = new CrosshairWindow();

        _keyboardHook = new KeyboardHook();
        _keyboardHook.Install(window);

        app.Run(window);

        app.Exit += (sender, args) => _keyboardHook.Uninstall();
    }
}