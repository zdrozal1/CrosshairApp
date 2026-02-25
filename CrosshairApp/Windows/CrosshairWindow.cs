using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using CrosshairApp.Utils;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace CrosshairApp.Windows;

public class CrosshairWindow : Window
{
    private const int WsExTransparent = 0x00000020;
    private const int GwlExstyle = -20;
    private const string TrayIconText = "Crosshair Application";

    private readonly Canvas _root;
    private NotifyIcon _notifyIcon;
    private SettingsWindow _settingsWindow;
    private DispatcherTimer _colorUpdateTimer;
    private DispatcherTimer _processCheckTimer;

    private string _currentProfileName;
    private bool _adsEnabledForProfile;
    private VisualConfiguration _hipfireConfig;
    private VisualConfiguration _adsConfig;
    private VisualConfiguration _activeConfig;

    private bool _isAdsActive;
    private bool _isPreviewMode;
    private bool _isPreviewingAds;
    private string _targetProcessName;
    private bool _userHidden;
    private bool _processCheckEnabled;

    private readonly System.Drawing.Bitmap _pixelBuffer = new(40, 40, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
    private DateTime _lastColorUpdate = DateTime.MinValue;
    private Color _lastAverageBackground = Colors.Black;
    private readonly Brush[] _neonCandidates =
    {
        Brushes.Red, Brushes.Lime, Brushes.Blue, Brushes.Yellow, Brushes.Cyan, Brushes.Magenta
    };

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    public CrosshairWindow()
    {
        InitializeWindow();
        UseLayoutRounding = true;
        CreateTrayIcon();

        _root = new Canvas { IsHitTestVisible = false };
        Content = _root;

        _hipfireConfig = new VisualConfiguration();
        _adsConfig = new VisualConfiguration();
        _activeConfig = _hipfireConfig;

        _currentProfileName = ConfigUtils.GetActiveProfile();
        LoadProfile(_currentProfileName);

        MouseHook.Start();
        MouseHook.RightMouseDown += OnRightMouseDown;
        MouseHook.RightMouseUp += OnRightMouseUp;

        Closed += (s, e) => MouseHook.Stop();

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);

            _processCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _processCheckTimer.Tick += CheckActiveWindow;
            _processCheckTimer.Start();

            _colorUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            _colorUpdateTimer.Tick += UpdateDynamicColor;
            _colorUpdateTimer.Start();
        };
    }

    public void LoadProfile(string profileName)
    {
        _currentProfileName = profileName;

        LoadConfigInto(profileName, _hipfireConfig, "");
        LoadConfigInto(profileName, _adsConfig, "ADS_");

        _targetProcessName = ConfigUtils.ConfigRead(profileName, "TargetProcess", "FortniteClient-Win64-Shipping.exe");
        bool.TryParse(ConfigUtils.ConfigRead(profileName, "ProcessCheckEnabled", "False"), out _processCheckEnabled);
        bool.TryParse(ConfigUtils.ConfigRead(profileName, "AdsEnabled", "False"), out _adsEnabledForProfile);

        ResolveActiveConfiguration();
        CheckActiveWindow(null, null);
    }

    private void LoadConfigInto(string profile, VisualConfiguration config, string prefix)
    {
        config.CrosshairColor = SafeBrush(ConfigUtils.ConfigRead(profile, $"{prefix}CrosshairColor", "#FFFF0000"));
        config.OutlineColor = SafeBrush(ConfigUtils.ConfigRead(profile, $"{prefix}OutlineColor", "#FF000000"));

        if (Enum.TryParse(ConfigUtils.ConfigRead(profile, $"{prefix}CrosshairStyle", "StandardCross"), true, out CrosshairStyle style))
            config.CrosshairStyle = style;
        else
            config.CrosshairStyle = CrosshairStyle.StandardCross;

        config.CrosshairGap = SafeDouble(profile, $"{prefix}CrosshairGap", "9.7");
        config.CrosshairLength = SafeDouble(profile, $"{prefix}CrosshairLength", "8");
        config.CrosshairLengthY = SafeDouble(profile, $"{prefix}CrosshairLengthY", "8");
        config.CrosshairOpacity = SafeDouble(profile, $"{prefix}CrosshairOpacity", "1.0");
        config.LineThickness = SafeDouble(profile, $"{prefix}LineThickness", "3.4");
        config.LineThicknessY = SafeDouble(profile, $"{prefix}LineThicknessY", "3.4");
        config.OutlineThickness = SafeDouble(profile, $"{prefix}OutlineThickness", "1.0");
        config.RotationAngle = SafeDouble(profile, $"{prefix}RotationAngle", "0");
        config.XOffset = SafeDouble(profile, $"{prefix}XOffset", "0");
        config.YOffset = SafeDouble(profile, $"{prefix}YOffset", "0");

        bool.TryParse(ConfigUtils.ConfigRead(profile, $"{prefix}DynamicColorEnabled", "False"), out bool dyn);
        config.DynamicColorEnabled = dyn;

        if (config.DynamicColorEnabled)
        {
            var centerX = (int)SystemParameters.PrimaryScreenWidth / 2;
            var centerY = (int)SystemParameters.PrimaryScreenHeight / 2;
            var avgColor = GetAverageScreenColor(centerX, centerY, 20, 5);
            config.CrosshairColor = GetBestContrastBrush(avgColor, config.CrosshairColor, true);
        }
    }

    private static Brush SafeBrush(string hex)
    {
        try { return (SolidColorBrush)new BrushConverter().ConvertFrom(hex); }
        catch { return Brushes.Red; }
    }

    private static double SafeDouble(string profile, string key, string def)
    {
        return Convert.ToDouble(ConfigUtils.ConfigRead(profile, key, def));
    }

    private void ResolveActiveConfiguration()
    {
        if (_isPreviewMode)
        {
            _activeConfig = _isPreviewingAds ? _adsConfig : _hipfireConfig;
        }
        else
        {
            _activeConfig = (_isAdsActive && _adsEnabledForProfile) ? _adsConfig : _hipfireConfig;
        }

        UpdateCrosshairVisuals();
    }

    public void SetPreviewMode(bool enabled, bool showAds)
    {
        _isPreviewMode = enabled;
        _isPreviewingAds = showAds;
        ResolveActiveConfiguration();
    }

    public void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible)
        {
            Visibility = Visibility.Collapsed;
            _userHidden = true;
        }
        else
        {
            Visibility = Visibility.Visible;
            _userHidden = false;
        }
    }

    public void SwitchToNextProfile()
    {
        CycleProfile(1);
    }

    public void SwitchToPreviousProfile()
    {
        CycleProfile(-1);
    }

    private void CycleProfile(int direction)
    {
        var profiles = ConfigUtils.GetProfileList();
        if (profiles == null || profiles.Count == 0) return;

        var currentIndex = profiles.FindIndex(p => p.Equals(_currentProfileName, StringComparison.OrdinalIgnoreCase));
        var nextIndex = (currentIndex + direction + profiles.Count) % profiles.Count;

        LoadProfile(profiles[nextIndex]);
        _settingsWindow?.LoadCurrentProfileData(profiles[nextIndex]);
    }

    private void OnRightMouseDown(object sender, EventArgs e)
    {
        if (!_adsEnabledForProfile || _isPreviewMode) return;
        _isAdsActive = true;
        ResolveActiveConfiguration();
    }

    private void OnRightMouseUp(object sender, EventArgs e)
    {
        if (_isPreviewMode) return;
        _isAdsActive = false;
        ResolveActiveConfiguration();
    }

    private void UpdateCrosshairVisuals()
    {
        var c = _activeConfig;
        double maxWidth;
        double maxHeight;


        if (c.CrosshairStyle == CrosshairStyle.Circle || c.CrosshairStyle == CrosshairStyle.Dot)
        {


            var size = c.CrosshairLength * 4 + c.OutlineThickness * 2 + c.LineThickness;
            maxWidth = size;
            maxHeight = size;
        }
        else
        {
            maxWidth = c.CrosshairLength * 4 + c.OutlineThickness * 2;
            maxHeight = c.CrosshairLengthY * 4 + c.OutlineThickness * 2;
        }

        Width = Math.Max(maxWidth, 10);
        Height = Math.Max(maxHeight, 10);

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2 + c.XOffset;
        Top = (SystemParameters.PrimaryScreenHeight - Height) / 2 + c.YOffset;

        _root.Children.Clear();
        AddCrosshair(_root);
    }

    private void AddCrosshair(Canvas root)
    {
        switch (_activeConfig.CrosshairStyle)
        {
            case CrosshairStyle.StandardCross: AddStandardCross(root); break;
            case CrosshairStyle.Tee: AddTeeCross(root); break;
            case CrosshairStyle.Circle: AddCircleCrosshair(root); break;
            case CrosshairStyle.Dot: AddDotCrosshair(root); break;
            default: AddStandardCross(root); break;
        }



        ApplyRotation(root);
    }

    private void AddDotCrosshair(Canvas root)
    {

        AddDotWithOutline(root, _activeConfig.CrosshairLength);
    }

    private void AddDotWithOutline(Canvas root, double diameter)
    {
        var c = _activeConfig;
        var centerX = Width / 2;
        var centerY = Height / 2;



        if (c.OutlineThickness > 0)
        {
            var outlineDiameter = diameter + (c.OutlineThickness * 2);
            var outline = new Ellipse
            {
                Width = outlineDiameter,
                Height = outlineDiameter,
                Fill = c.OutlineColor,
                Opacity = c.CrosshairOpacity,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(outline, centerX - (outlineDiameter / 2));
            Canvas.SetTop(outline, centerY - (outlineDiameter / 2));
            root.Children.Add(outline);
        }


        var dot = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = c.CrosshairColor,
            Opacity = c.CrosshairOpacity,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(dot, centerX - (diameter / 2));
        Canvas.SetTop(dot, centerY - (diameter / 2));
        root.Children.Add(dot);
    }

    private void AddStandardCross(Canvas root)
    {
        var c = _activeConfig;
        AddLineWithOutline(root, 0, -c.CrosshairLengthY - c.CrosshairGap / 2, 0, -c.CrosshairGap / 2, c.LineThicknessY);
        AddLineWithOutline(root, 0, c.CrosshairGap / 2, 0, c.CrosshairLengthY + c.CrosshairGap / 2, c.LineThicknessY);
        AddLineWithOutline(root, -c.CrosshairLength - c.CrosshairGap / 2, 0, -c.CrosshairGap / 2, 0, c.LineThickness);
        AddLineWithOutline(root, c.CrosshairGap / 2, 0, c.CrosshairLength + c.CrosshairGap / 2, 0, c.LineThickness);
    }

    private void AddTeeCross(Canvas root)
    {
        var c = _activeConfig;
        AddLineWithOutline(root, 0, c.CrosshairGap / 2, 0, c.CrosshairLengthY + c.CrosshairGap / 2, c.LineThicknessY);
        AddLineWithOutline(root, -c.CrosshairLength - c.CrosshairGap / 2, 0, -c.CrosshairGap / 2, 0, c.LineThickness);
        AddLineWithOutline(root, c.CrosshairGap / 2, 0, c.CrosshairLength + c.CrosshairGap / 2, 0, c.LineThickness);
    }

    private void AddCircleCrosshair(Canvas root)
    {


        double diameter = _activeConfig.CrosshairLength * 2;
        AddCircleWithOutline(root, diameter);
    }

    private void AddCircleWithXCrosshair(Canvas root)
    {
        var c = _activeConfig;
        AddCircleWithOutline(root, c.CrosshairLength * 2);
        var adjustedLength = c.CrosshairLength - c.CrosshairGap;
        AddLineWithOutline(root, -adjustedLength, -adjustedLength, adjustedLength, adjustedLength, c.LineThickness);
        AddLineWithOutline(root, adjustedLength, -adjustedLength, -adjustedLength, adjustedLength, c.LineThickness);
    }

    private void AddLineWithOutline(Canvas root, double x1, double y1, double x2, double y2, double thickness)
    {
        var c = _activeConfig;
        var centerX = Width / 2;
        var centerY = Height / 2;

        var outline = new Line
        {
            X1 = x1 + centerX,
            Y1 = y1 + centerY,
            X2 = x2 + centerX,
            Y2 = y2 + centerY,
            Stroke = c.OutlineColor,
            StrokeThickness = thickness + c.OutlineThickness * 2,
            Opacity = c.CrosshairOpacity,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        var line = new Line
        {
            X1 = x1 + centerX,
            Y1 = y1 + centerY,
            X2 = x2 + centerX,
            Y2 = y2 + centerY,
            Stroke = c.CrosshairColor,
            StrokeThickness = thickness,
            Opacity = c.CrosshairOpacity,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        root.Children.Add(outline);
        root.Children.Add(line);
    }

    private void AddCircleWithOutline(Canvas root, double diameter)
    {
        var c = _activeConfig;
        var centerX = Width / 2;
        var centerY = Height / 2;






        if (c.OutlineThickness > 0)
        {
            var outlineThickness = c.LineThickness + (c.OutlineThickness * 2);
            var outlineDiameter = diameter + (c.OutlineThickness * 2);

            var outlineCircle = new Ellipse
            {
                Width = outlineDiameter,
                Height = outlineDiameter,
                Stroke = c.OutlineColor,
                StrokeThickness = outlineThickness,
                Opacity = c.CrosshairOpacity,
                IsHitTestVisible = false
            };


            Canvas.SetLeft(outlineCircle, centerX - (outlineDiameter / 2));
            Canvas.SetTop(outlineCircle, centerY - (outlineDiameter / 2));
            root.Children.Add(outlineCircle);
        }


        var mainCircle = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Stroke = c.CrosshairColor,
            StrokeThickness = c.LineThickness,
            Opacity = c.CrosshairOpacity,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(mainCircle, centerX - (diameter / 2));
        Canvas.SetTop(mainCircle, centerY - (diameter / 2));
        root.Children.Add(mainCircle);
    }

    private void ApplyRotation(Canvas root)
    {
        root.RenderTransform = new RotateTransform(_activeConfig.RotationAngle, Width / 2, Height / 2);
    }

    private void UpdateDynamicColor(object sender, EventArgs e)
    {
        try
        {
            if (_activeConfig == null || !_activeConfig.DynamicColorEnabled || Visibility != Visibility.Visible) return;
            if ((DateTime.Now - _lastColorUpdate).TotalMilliseconds < 200) return;
            _lastColorUpdate = DateTime.Now;

            var centerX = (int)SystemParameters.PrimaryScreenWidth / 2;
            var centerY = (int)SystemParameters.PrimaryScreenHeight / 2;
            var avgColor = GetAverageScreenColor(centerX, centerY, 20, 5);

            if (GetColorDistance(avgColor, _lastAverageBackground) < 1500) return;
            _lastAverageBackground = avgColor;

            var bestBrush = GetBestContrastBrush(avgColor, _activeConfig.CrosshairColor, false);

            if (_activeConfig.CrosshairColor is SolidColorBrush currentSolid)
            {
                if (!_neonCandidates.Any(x => x is SolidColorBrush sb && sb.Color == currentSolid.Color))
                {
                    _activeConfig.CrosshairColor = bestBrush;
                    UpdateCrosshairVisuals();
                    return;
                }
            }
            SmoothColorTransition(bestBrush);
        }
        catch (Exception)
        {
        }
    }

    private Color GetAverageScreenColor(int centerX, int centerY, int radius, int step)
    {
        int width = radius * 2;
        int height = radius * 2;
        int left = centerX - radius;
        int top = centerY - radius;
        if (left < 0 || top < 0) return Colors.Black;

        try
        {
            using (var g = System.Drawing.Graphics.FromImage(_pixelBuffer))
            {
                g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
            }
            var rect = new System.Drawing.Rectangle(0, 0, width, height);
            var bmd = _pixelBuffer.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, _pixelBuffer.PixelFormat);
            long rSum = 0, gSum = 0, bSum = 0;
            int count = 0;
            int pixelSize = 4;
            unsafe
            {
                byte* ptr = (byte*)bmd.Scan0;
                for (int y = 0; y < height; y += step)
                {
                    for (int x = 0; x < width; x += step)
                    {
                        int offset = (y * bmd.Stride) + (x * pixelSize);
                        bSum += ptr[offset];
                        gSum += ptr[offset + 1];
                        rSum += ptr[offset + 2];
                        count++;
                    }
                }
            }
            _pixelBuffer.UnlockBits(bmd);
            if (count == 0) return Colors.Black;
            return Color.FromRgb((byte)(rSum / count), (byte)(gSum / count), (byte)(bSum / count));
        }
        catch { return Colors.Black; }
    }

    private Brush GetBestContrastBrush(Color background, Brush currentBrush, bool ignoreHysteresis)
    {
        Brush bestBrush = _neonCandidates[0];
        double maxDiff = -1;
        double backgroundBrightness = background.R + background.G + background.B;
        bool isBrightBackground = backgroundBrightness > 600;
        double redBiasFactor = isBrightBackground ? 1.4 : 1.0;

        foreach (var brush in _neonCandidates)
        {
            var solid = (SolidColorBrush)brush;
            double dist = GetColorDistance(solid.Color, background);
            if (solid.Color == Colors.Red && isBrightBackground) dist *= redBiasFactor;

            if (dist > maxDiff)
            {
                maxDiff = dist;
                bestBrush = brush;
            }
        }

        if (!ignoreHysteresis && currentBrush is SolidColorBrush currentSolid)
        {
            double currentDist = GetColorDistance(currentSolid.Color, background);
            if (currentSolid.Color == Colors.Red && isBrightBackground) currentDist *= redBiasFactor;
            if (maxDiff < currentDist * 1.25) return currentBrush;
        }
        return bestBrush;
    }

    private static double GetColorDistance(Color c1, Color c2)
    {
        long rDiff = c1.R - c2.R;
        long gDiff = c1.G - c2.G;
        long bDiff = c1.B - c2.B;
        return (rDiff * rDiff) + (gDiff * gDiff) + (bDiff * bDiff);
    }

    private void SmoothColorTransition(Brush newBrush)
    {
        if (_activeConfig.CrosshairColor is not SolidColorBrush currentBrush || newBrush is not SolidColorBrush targetBrush) return;
        if (currentBrush.Color == targetBrush.Color) return;

        var freshBrush = new SolidColorBrush(currentBrush.Color);
        _activeConfig.CrosshairColor = freshBrush;

        var colorAnimation = new ColorAnimation
        {
            From = currentBrush.Color,
            To = targetBrush.Color,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            FillBehavior = FillBehavior.HoldEnd
        };

        colorAnimation.Completed += (s, e) =>
        {
            _activeConfig.CrosshairColor = targetBrush;
            UpdateCrosshairVisuals();
        };

        freshBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        UpdateCrosshairVisuals();
    }

    public void UpdateTargetProcess(string processName)
    {
        _targetProcessName = processName;
    }

    public void SetProcessCheckEnabled(bool enabled)
    {
        _processCheckEnabled = enabled;
        CheckActiveWindow(null, null);
    }

    private void CheckActiveWindow(object sender, EventArgs e)
    {
        if (_userHidden) return;
        bool shouldBeVisible = false;
        if (!_processCheckEnabled || string.IsNullOrWhiteSpace(_targetProcessName))
        {
            shouldBeVisible = true;
        }
        else
        {
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hWnd, out int processId);
                    using (var process = Process.GetProcessById(processId))
                    {
                        string currentProcName = process.ProcessName;
                        string target = _targetProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                            ? _targetProcessName.Substring(0, _targetProcessName.Length - 4)
                            : _targetProcessName;

                        if (currentProcName.Equals(target, StringComparison.OrdinalIgnoreCase))
                        {
                            shouldBeVisible = true;
                        }
                    }
                }
            }
            catch { shouldBeVisible = false; }
        }

        if (shouldBeVisible)
        {
            if (Visibility != Visibility.Visible) Visibility = Visibility.Visible;
            var windowHandle = new WindowInteropHelper(this).Handle;
            SetWindowPos(windowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        else
        {
            if (Visibility == Visibility.Visible) Visibility = Visibility.Collapsed;
        }
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

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Settings", null, (_, _) => ShowSettingsWindow());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon(stream),
            Visible = true,
            Text = TrayIconText,
            ContextMenuStrip = contextMenu
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettingsWindow();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is not { IsLoaded: true })
        {
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Closed += (s, e) =>
            {
                _settingsWindow = null;
                SetPreviewMode(false, false);
            };
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        Application.Current.Shutdown();
    }

    private static void SetWindowExTransparent(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GwlExstyle);
        SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent);
    }
    private class VisualConfiguration
    {
        public Brush CrosshairColor = Brushes.Red;
        public Brush OutlineColor = Brushes.Black;
        public CrosshairStyle CrosshairStyle = CrosshairStyle.StandardCross;
        public double CrosshairGap = 9.7;
        public double CrosshairLength = 8.0;
        public double CrosshairLengthY = 8.0;
        public double CrosshairOpacity = 1.0;
        public double LineThickness = 3.4;
        public double LineThicknessY = 3.4;
        public double OutlineThickness = 1.0;
        public double RotationAngle = 0;
        public double XOffset = 0;
        public double YOffset = 0;
        public bool DynamicColorEnabled = false;
    }
}