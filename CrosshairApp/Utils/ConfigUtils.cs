using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CrosshairApp.Utils;

public abstract class ConfigUtils
{
    private const string ActiveProfileKey = "ActiveProfile";
    private const string DefaultProfileName = "Default";
    private const string EnableAdsProfileKey = "EnableAdsProfile";
    private const string AdsProfileNameKey = "AdsProfileName";

    private static Dictionary<string, string> ReadAllProperties()
    {
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(Launcher.ConfigFilePath))
            foreach (var line in File.ReadLines(Launcher.ConfigFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2) properties[parts[0].Trim()] = parts[1].Trim();
            }

        return properties;
    }

    private static void WriteAllProperties(Dictionary<string, string> properties)
    {
        using (var writer = new StreamWriter(Launcher.ConfigFilePath, false, Encoding.UTF8))
        {
            foreach (var kvp in properties) writer.WriteLine($"{kvp.Key}={kvp.Value}");
        }
    }

    public static void ConfigWrite(string profileName, string property, string value)
    {
        var allProperties = ReadAllProperties();
        allProperties[$"{profileName}.{property}"] = value;
        WriteAllProperties(allProperties);
    }

    public static string ConfigRead(string profileName, string property, string defaultValue = null)
    {
        var allProperties = ReadAllProperties();
        if (allProperties.TryGetValue($"{profileName}.{property}", out var value)) return value;
        return defaultValue;
    }

    public static string GetActiveProfile()
    {
        var allProperties = ReadAllProperties();
        if (allProperties.TryGetValue(ActiveProfileKey, out var profileName) && !string.IsNullOrWhiteSpace(profileName))
            return profileName;
        return DefaultProfileName;
    }

    public static void SetActiveProfile(string profileName)
    {
        var allProperties = ReadAllProperties();
        allProperties[ActiveProfileKey] = profileName;
        WriteAllProperties(allProperties);
    }

    public static List<string> GetProfileList()
    {
        var allProperties = ReadAllProperties();
        var profileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in allProperties.Keys)
            if (key.Contains("."))
                profileNames.Add(key.Split('.')[0]);

        if (!profileNames.Any()) profileNames.Add(DefaultProfileName);
        return profileNames.ToList();
    }

    public static void CreateProfile(string profileName)
    {
        CheckAndSetDefaultValues(profileName);
        if (GetProfileList().Count == 1) SetActiveProfile(profileName);
    }

    public static void DeleteProfile(string profileName)
    {
        if (profileName.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase)) return;

        var allProperties = ReadAllProperties();
        var keysToRemove = allProperties.Keys.Where(k => k.StartsWith($"{profileName}.")).ToList();
        foreach (var key in keysToRemove) allProperties.Remove(key);

        if (GetActiveProfile().Equals(profileName, StringComparison.OrdinalIgnoreCase))
            allProperties[ActiveProfileKey] = DefaultProfileName;

        WriteAllProperties(allProperties);
    }

    public static void CreateConfig()
    {
        using (var writer = new StreamWriter(Launcher.ConfigFilePath, false, Encoding.UTF8))
        {
            writer.WriteLine("# Configuration Properties");
        }

        CheckAndSetDefaultValues(DefaultProfileName);
        SetActiveProfile(DefaultProfileName);
    }

    private static void CheckAndSetDefaultValues(string profileName)
    {
        ConfigWrite(profileName, "CrosshairColor", "#FFFF0000");
        ConfigWrite(profileName, "OutlineColor", "#FF000000");
        ConfigWrite(profileName, "CrosshairStyle", "StandardCross");
        ConfigWrite(profileName, "CrosshairGap", "9.7");
        ConfigWrite(profileName, "CrosshairLength", "8");
        ConfigWrite(profileName, "CrosshairLengthY", "8");
        ConfigWrite(profileName, "CrosshairOpacity", "1.0");
        ConfigWrite(profileName, "LineThickness", "3.4");
        ConfigWrite(profileName, "LineThicknessY", "3.4");
        ConfigWrite(profileName, "OutlineThickness", "1.0");
        ConfigWrite(profileName, "RotationAngle", "0");
        ConfigWrite(profileName, "XOffset", "0");
        ConfigWrite(profileName, "YOffset", "0");
    }

    public static bool GetEnableAdsProfile()
    {
        var allProperties = ReadAllProperties();
        if (allProperties.TryGetValue(EnableAdsProfileKey, out var value) && bool.TryParse(value, out var enabled))
            return enabled;
        return false;
    }

    public static void SetEnableAdsProfile(bool enabled)
    {
        var allProperties = ReadAllProperties();
        allProperties[EnableAdsProfileKey] = enabled.ToString();
        WriteAllProperties(allProperties);
    }

    public static string GetAdsProfile()
    {
        var allProperties = ReadAllProperties();
        if (allProperties.TryGetValue(AdsProfileNameKey, out var profileName) && !string.IsNullOrWhiteSpace(profileName))
            return profileName;
        return GetActiveProfile();
    }

    public static void SetAdsProfile(string profileName)
    {
        var allProperties = ReadAllProperties();
        allProperties[AdsProfileNameKey] = profileName;
        WriteAllProperties(allProperties);
    }
}