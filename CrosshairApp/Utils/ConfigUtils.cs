using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CrosshairApp.Utils;

public static class ConfigUtils
{
    private const string ActiveProfileKey = "ActiveProfile";
    private const string DefaultProfileName = "Default";
    private const string EnableAdsProfileKey = "EnableAdsProfile";
    private const string AdsProfileNameKey = "AdsProfileName";

    private static readonly Dictionary<string, string> _configCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isDirty;

    static ConfigUtils()
    {
        LoadConfig();
    }

    public static void LoadConfig()
    {
        _configCache.Clear();

        if (string.IsNullOrWhiteSpace(Launcher.ConfigFilePath)) return;

        if (!File.Exists(Launcher.ConfigFilePath))
        {
            CreateConfig();
            return;
        }

        try
        {
            foreach (var line in File.ReadLines(Launcher.ConfigFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    _configCache[parts[0].Trim()] = parts[1].Trim();
                }
            }
            _isDirty = false;
        }
        catch (Exception)
        {
            CheckAndSetDefaultValues(DefaultProfileName);
        }
    }

    public static void SaveConfig()
    {
        if (!_isDirty || string.IsNullOrWhiteSpace(Launcher.ConfigFilePath)) return;

        try
        {
            using var writer = new StreamWriter(Launcher.ConfigFilePath, false, Encoding.UTF8);
            writer.WriteLine("# Configuration Properties");
            foreach (var kvp in _configCache)
            {
                if (kvp.Key != null && kvp.Value != null)
                {
                    writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
            }
            _isDirty = false;
        }
        catch (Exception)
        {
        }
    }

    public static void ConfigWrite(string profileName, string property, string value)
    {
        var key = $"{profileName}.{property}";
        if (_configCache.TryGetValue(key, out var current) && current == value) return;

        _configCache[key] = value;
        _isDirty = true;
    }

    public static string ConfigRead(string profileName, string property, string defaultValue = null)
    {
        return _configCache.TryGetValue($"{profileName}.{property}", out var value) ? value : defaultValue;
    }

    public static string GetActiveProfile()
    {
        return _configCache.TryGetValue(ActiveProfileKey, out var val) && !string.IsNullOrWhiteSpace(val)
            ? val
            : DefaultProfileName;
    }

    public static void SetActiveProfile(string profileName)
    {
        if (_configCache.TryGetValue(ActiveProfileKey, out var current) && current == profileName) return;
        _configCache[ActiveProfileKey] = profileName;
        _isDirty = true;
    }

    public static List<string> GetProfileList()
    {
        var profileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _configCache.Keys)
        {
            if (key.Contains('.'))
            {
                profileNames.Add(key.Split('.')[0]);
            }
        }

        if (profileNames.Count == 0) profileNames.Add(DefaultProfileName);
        return profileNames.ToList();
    }

    public static void CreateProfile(string profileName)
    {
        CheckAndSetDefaultValues(profileName);
        if (GetProfileList().Count == 1) SetActiveProfile(profileName);
        SaveConfig();
    }

    public static void DeleteProfile(string profileName)
    {
        if (profileName.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase)) return;

        var keysToRemove = _configCache.Keys.Where(k => k.StartsWith($"{profileName}.")).ToList();
        foreach (var key in keysToRemove)
        {
            _configCache.Remove(key);
        }

        if (GetActiveProfile().Equals(profileName, StringComparison.OrdinalIgnoreCase))
        {
            SetActiveProfile(DefaultProfileName);
        }
        _isDirty = true;
        SaveConfig();
    }

    public static void CreateConfig()
    {
        _configCache.Clear();
        CheckAndSetDefaultValues(DefaultProfileName);
        SetActiveProfile(DefaultProfileName);
        _isDirty = true;
        SaveConfig();
    }

    public static void CheckAndSetDefaultValues(string profileName)
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
        ConfigWrite(profileName, "TargetProcess", "FortniteClient-Win64-Shipping.exe");
        ConfigWrite(profileName, "ProcessCheckEnabled", "False");
        ConfigWrite(profileName, "DynamicColorEnabled", "False");
    }

    public static bool GetEnableAdsProfile()
    {
        return _configCache.TryGetValue(EnableAdsProfileKey, out var val) && bool.TryParse(val, out var enabled) && enabled;
    }

    public static void SetEnableAdsProfile(bool enabled)
    {
        _configCache[EnableAdsProfileKey] = enabled.ToString();
        _isDirty = true;
    }

    public static string GetAdsProfile()
    {
        return _configCache.TryGetValue(AdsProfileNameKey, out var val) && !string.IsNullOrWhiteSpace(val)
            ? val
            : GetActiveProfile();
    }

    public static void SetAdsProfile(string profileName)
    {
        _configCache[AdsProfileNameKey] = profileName;
        _isDirty = true;
    }
}