using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace CrosshairApp.Utils;

public class ConfigUtils
{
    public static void ConfigWrite(string property, string value)
    {
        var configProperties = new Dictionary<string, string>();

        if (File.Exists(Launcher.ConfigFilePath))
            foreach (var line in File.ReadLines(Launcher.ConfigFilePath))
                if (!string.IsNullOrWhiteSpace(line) && line.Contains("="))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length == 2) configProperties[parts[0].Trim()] = parts[1].Trim();
                }

        configProperties[property] = value;

        using (var writer = new StreamWriter(Launcher.ConfigFilePath, false, Encoding.UTF8))
        {
            foreach (var kvp in configProperties) writer.WriteLine($"{kvp.Key}={kvp.Value}");
        }
    }

    public static string ConfigRead(string property)
    {
        try
        {
            var prop = new NameValueCollection();
            if (File.Exists(Launcher.ConfigFilePath))
            {
                foreach (var line in File.ReadLines(Launcher.ConfigFilePath))
                {
                    var keyValue = line.Split(new[] { '=' }, 2);
                    if (keyValue.Length == 2) prop[keyValue[0]] = keyValue[1];
                }

                return prop[property];
            }

            throw new IOException("Config file not found.");
        }
        catch (Exception ex)
        {
            throw new IOException("Error reading config.properties file.", ex);
        }
    }

    public static void CreateConfig()
    {
        if (!File.Exists(Launcher.ConfigFilePath))
            using (var writer = new StreamWriter(Launcher.ConfigFilePath, false, Encoding.UTF8))
            {
                writer.WriteLine("# Configuration Properties");
            }

        CheckAndSetDefaultValues();
    }


    private static void CheckAndSetDefaultValue(string property, string defaultValue)
    {
        var prop = new NameValueCollection();

        if (File.Exists(Launcher.ConfigFilePath))
        {
            foreach (var line in File.ReadLines(Launcher.ConfigFilePath))
            {
                var keyValue = line.Split(new[] { '=' }, 2);
                if (keyValue.Length == 2) prop[keyValue[0]] = keyValue[1];
            }

            if (prop[property] == null) prop[property] = defaultValue;

            using (var sw = new StreamWriter(Launcher.ConfigFilePath))
            {
                foreach (var key in prop.AllKeys) sw.WriteLine($"{key}={prop[key]}");
            }
        }
    }

    public static void CheckAndSetDefaultValues()
    {
        CheckAndSetDefaultValue("CrosshairColor", "Red");
        CheckAndSetDefaultValue("CrosshairStyle", "StandardCross");
        CheckAndSetDefaultValue("CrosshairGap", "9.7");
        CheckAndSetDefaultValue("CrosshairLength", "8");
        CheckAndSetDefaultValue("CrosshairOpacity", "1.0");
        CheckAndSetDefaultValue("LineThickness", "3.4");
        CheckAndSetDefaultValue("RotationAngle", "0");
        CheckAndSetDefaultValue("XOffset", "0");
        CheckAndSetDefaultValue("YOffset", "0");
    }
}