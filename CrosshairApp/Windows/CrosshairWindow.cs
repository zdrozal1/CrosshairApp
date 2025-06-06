using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using CrosshairApp.Utils;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

namespace CrosshairApp.Windows;

public class CrosshairWindow : Window
{
    private const int WsExTransparent = 0x00000020;
    private const int GwlExstyle = -20;
    private const string TrayIconText = "Crosshair Application";

    private readonly Canvas _root;
    private string _activeProfile;
    private string _adsProfileName;
    private bool _isAdsActive;
    private bool _isAdsEnabled;
    private NotifyIcon _notifyIcon;
    private string _originalProfileName;
    private SettingsWindow _settingsWindow;

    public CrosshairWindow()
    {
        InitializeWindow();
        CreateTrayIcon();

        _root = new Canvas { IsHitTestVisible = false };
        Content = _root;

        _isAdsEnabled = ConfigUtils.GetEnableAdsProfile();
        _adsProfileName = ConfigUtils.GetAdsProfile();

        _originalProfileName = ConfigUtils.GetActiveProfile();
        LoadProfile(_originalProfileName);

        MouseHook.Start();
        MouseHook.RightMouseDown += OnRightMouseDown;
        MouseHook.RightMouseUp += OnRightMouseUp;

        Closed += (s, e) => MouseHook.Stop();

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        };
    }

    private Brush CrosshairColor { get; set; }
    private Brush OutlineColor { get; set; }
    private double CrosshairGap { get; set; }
    private double CrosshairLength { get; set; }
    private double CrosshairLengthY { get; set; }
    private double CrosshairOpacity { get; set; }
    private CrosshairStyle CrosshairStyle { get; set; }
    private double LineThickness { get; set; }
    private double LineThicknessY { get; set; }
    private double OutlineThickness { get; set; }
    private double RotationAngle { get; set; }
    private double XOffset { get; set; }
    private double YOffset { get; set; }

    private void LoadProfile(string profileName)
    {
        _activeProfile = profileName;

        CrosshairColor =
            (SolidColorBrush)new BrushConverter().ConvertFrom(ConfigUtils.ConfigRead(_activeProfile, "CrosshairColor",
                "#FFFF0000"));
        OutlineColor =
            (SolidColorBrush)new BrushConverter().ConvertFrom(ConfigUtils.ConfigRead(_activeProfile, "OutlineColor",
                "#FF000000"));
        var crosshairStyleStr = ConfigUtils.ConfigRead(_activeProfile, "CrosshairStyle", "StandardCross");
        CrosshairStyle = (CrosshairStyle)Enum.Parse(typeof(CrosshairStyle), crosshairStyleStr, true);
        CrosshairGap = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "CrosshairGap", "9.7"));
        CrosshairLength = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "CrosshairLength", "8"));
        CrosshairLengthY = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "CrosshairLengthY", "8"));
        CrosshairOpacity = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "CrosshairOpacity", "1.0"));
        LineThickness = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "LineThickness", "3.4"));
        LineThicknessY = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "LineThicknessY", "3.4"));
        OutlineThickness = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "OutlineThickness", "1.0"));
        RotationAngle = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "RotationAngle", "0"));
        XOffset = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "XOffset", "0"));
        YOffset = Convert.ToDouble(ConfigUtils.ConfigRead(_activeProfile, "YOffset", "0"));

        UpdateCrosshairVisuals();
    }

    public void ToggleVisibility()
    {
        Visibility = Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    public void SwitchToNextProfile()
    {
        var profiles = ConfigUtils.GetProfileList();
        var currentIndex = profiles.IndexOf(_activeProfile);
        var nextIndex = (currentIndex + 1) % profiles.Count;
        LoadProfile(profiles[nextIndex]);
        _settingsWindow?.LoadProfileSettings(profiles[nextIndex]);
    }

    public void SwitchToPreviousProfile()
    {
        var profiles = ConfigUtils.GetProfileList();
        var currentIndex = profiles.IndexOf(_activeProfile);
        var prevIndex = (currentIndex - 1 + profiles.Count) % profiles.Count;
        LoadProfile(profiles[prevIndex]);
        _settingsWindow?.LoadProfileSettings(profiles[prevIndex]);
    }

    public void UpdateCrosshairProperties(Brush color, Brush outlineColor, CrosshairStyle style, double gap,
        double length, double lengthY, double opacity, double thickness, double thicknessY, double outline,
        double rotation, double xOffset, double yOffset)
    {
        CrosshairColor = color;
        OutlineColor = outlineColor;
        CrosshairStyle = style;
        CrosshairGap = gap;
        CrosshairLength = length;
        CrosshairLengthY = lengthY;
        CrosshairOpacity = opacity;
        LineThickness = thickness;
        LineThicknessY = thicknessY;
        OutlineThickness = outline;
        RotationAngle = rotation;
        XOffset = xOffset;
        YOffset = yOffset;

        UpdateCrosshairVisuals();
    }

    public void SwitchBaseProfile(string profileName)
    {
        _originalProfileName = profileName;
        ConfigUtils.SetActiveProfile(profileName);

        if (!_isAdsActive)
        {
            LoadProfile(_originalProfileName);
        }
    }

    private void UpdateCrosshairVisuals()
    {
        var maxWidth = CrosshairLength * 4 + OutlineThickness * 2;
        var maxHeight = CrosshairLengthY * 4 + OutlineThickness * 2;
        Width = maxWidth;
        Height = maxHeight;

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2 + XOffset;
        Top = (SystemParameters.PrimaryScreenHeight - Height) / 2 + YOffset;

        _root.Children.Clear();
        AddCrosshair(_root);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private static void SetWindowExTransparent(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GwlExstyle);
        SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent);
    }

    private void InitializeWindow()
    {
        Background = Brushes.Transparent;
        WindowStyle = WindowStyle.None;
        Topmost = true;
        AllowsTransparency = true;
        ShowInTaskbar = false;
    }

    private void CreateTrayIcon()
    {
        var resourceName = "CrosshairApp.Images.target.ico";
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return;
        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon(stream),
            Visible = true,
            Text = TrayIconText,
            ContextMenu = new ContextMenu([
                new MenuItem("Settings", (_, _) => ShowSettingsWindow()),
                new MenuItem("Exit", (_, _) => ExitApplication())
            ])
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettingsWindow();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is not { IsLoaded: true })
        {
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        Application.Current.Shutdown();
    }

    private void AddCrosshair(Canvas root)
    {
        switch (CrosshairStyle)
        {
            case CrosshairStyle.StandardCross:
                AddStandardCross(root);
                break;
            case CrosshairStyle.Tee:
                AddTeeCross(root);
                break;
            case CrosshairStyle.Circle:
                AddCircleCrosshair(root);
                break;
            case CrosshairStyle.Dot:
                AddDotCrosshair(root);
                break;
            case CrosshairStyle.CircleWithX:
                AddCircleWithXCrosshair(root);
                break;
            default:
                AddStandardCross(root);
                break;
        }

        ApplyRotation(root);
    }

    private void AddStandardCross(Canvas root)
    {
        AddLineWithOutline(root, 0, -CrosshairLengthY - CrosshairGap / 2, 0, -CrosshairGap / 2, LineThicknessY);
        AddLineWithOutline(root, 0, CrosshairGap / 2, 0, CrosshairLengthY + CrosshairGap / 2, LineThicknessY);
        AddLineWithOutline(root, -CrosshairLength - CrosshairGap / 2, 0, -CrosshairGap / 2, 0, LineThickness);
        AddLineWithOutline(root, CrosshairGap / 2, 0, CrosshairLength + CrosshairGap / 2, 0, LineThickness);
    }

    private void AddTeeCross(Canvas root)
    {
        AddLineWithOutline(root, 0, CrosshairGap / 2, 0, CrosshairLengthY + CrosshairGap / 2, LineThicknessY);
        AddLineWithOutline(root, -CrosshairLength - CrosshairGap / 2, 0, -CrosshairGap / 2, 0, LineThickness);
        AddLineWithOutline(root, CrosshairGap / 2, 0, CrosshairLength + CrosshairGap / 2, 0, LineThickness);
    }

    private void AddCircleCrosshair(Canvas root)
    {
        AddCircleWithOutline(root, CrosshairLength * 2);
    }

    private void AddDotCrosshair(Canvas root)
    {
        AddDotWithOutline(root, CrosshairLength / 2);
    }

    private void AddCircleWithXCrosshair(Canvas root)
    {
        AddCircleWithOutline(root, CrosshairLength * 2);
        var adjustedLength = CrosshairLength - CrosshairGap;
        AddLineWithOutline(root, -adjustedLength, -adjustedLength, adjustedLength, adjustedLength, LineThickness);
        AddLineWithOutline(root, adjustedLength, -adjustedLength, -adjustedLength, adjustedLength, LineThickness);
    }

    private void AddLineWithOutline(Canvas root, double x1, double y1, double x2, double y2, double thickness)
    {
        var centerX = Width / 2;
        var centerY = Height / 2;

        var outline = new Line
        {
            X1 = x1 + centerX, Y1 = y1 + centerY, X2 = x2 + centerX, Y2 = y2 + centerY,
            Stroke = OutlineColor, StrokeThickness = thickness + OutlineThickness * 2, Opacity = CrosshairOpacity,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
        };
        var line = new Line
        {
            X1 = x1 + centerX, Y1 = y1 + centerY, X2 = x2 + centerX, Y2 = y2 + centerY,
            Stroke = CrosshairColor, StrokeThickness = thickness, Opacity = CrosshairOpacity,
            StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round
        };
        root.Children.Add(outline);
        root.Children.Add(line);
    }

    private void AddCircleWithOutline(Canvas root, double diameter)
    {
        var centerX = Width / 2;
        var centerY = Height / 2;

        var outline = new Ellipse
        {
            Width = diameter, Height = diameter, Stroke = OutlineColor,
            StrokeThickness = LineThickness + OutlineThickness * 2, Opacity = CrosshairOpacity
        };
        var circle = new Ellipse
        {
            Width = diameter, Height = diameter, Stroke = CrosshairColor, StrokeThickness = LineThickness,
            Opacity = CrosshairOpacity
        };

        Canvas.SetLeft(outline, centerX - diameter / 2);
        Canvas.SetTop(outline, centerY - diameter / 2);
        Canvas.SetLeft(circle, centerX - diameter / 2);
        Canvas.SetTop(circle, centerY - diameter / 2);

        root.Children.Add(outline);
        root.Children.Add(circle);
    }

    private void AddDotWithOutline(Canvas root, double size)
    {
        var centerX = Width / 2;
        var centerY = Height / 2;

        var outline = new Ellipse
        {
            Width = size + OutlineThickness * 2, Height = size + OutlineThickness * 2, Fill = OutlineColor,
            Opacity = CrosshairOpacity
        };
        var dot = new Ellipse { Width = size, Height = size, Fill = CrosshairColor, Opacity = CrosshairOpacity };

        Canvas.SetLeft(outline, centerX - size / 2 - OutlineThickness);
        Canvas.SetTop(outline, centerY - size / 2 - OutlineThickness);
        Canvas.SetLeft(dot, centerX - size / 2);
        Canvas.SetTop(dot, centerY - size / 2);

        root.Children.Add(outline);
        root.Children.Add(dot);
    }

    private void ApplyRotation(Canvas root)
    {
        root.RenderTransform = new RotateTransform(RotationAngle, Width / 2, Height / 2);
    }

    public void SetAdsEnabled(bool isEnabled)
    {
        _isAdsEnabled = isEnabled;
        if (_isAdsEnabled || !_isAdsActive) return;
        _isAdsActive = false;
        LoadProfile(_originalProfileName);
    }

    public void SetAdsProfile(string profileName)
    {
        _adsProfileName = profileName;
        if (_isAdsEnabled && _isAdsActive)
        {
            LoadProfile(_adsProfileName);
        }
    }

    private void OnRightMouseDown(object sender, EventArgs e)
    {
        if (!_isAdsEnabled || _isAdsActive) return;
        _isAdsActive = true;
        LoadProfile(_adsProfileName);
    }

    private void OnRightMouseUp(object sender, EventArgs e)
    {
        if (!_isAdsActive) return;
        _isAdsActive = false;
        LoadProfile(_originalProfileName);
    }
}