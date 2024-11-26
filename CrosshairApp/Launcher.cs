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

    [STAThread]
    public static void Main()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyPath);

        ConfigFilePath = Path.Combine(assemblyDir, "config.properties");

        if (!File.Exists(ConfigFilePath)) ConfigUtils.CreateConfig();

        var app = new Application();
        var window = new CrosshairWindow();
        app.Run(window);
    }
}