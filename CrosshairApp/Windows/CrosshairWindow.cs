using CrosshairApp.Utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
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

    private DispatcherTimer _colorUpdateTimer;
    public bool DynamicColorEnabled { get; set; }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(IntPtr hdc, int x, int y);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    private DispatcherTimer _processCheckTimer;
    private string _targetProcessName;
    private bool _userHidden;
    private bool _processCheckEnabled;

    private readonly Brush[] _neonCandidates =
    {
        Brushes.Red,
        Brushes.Lime,
        Brushes.Blue,
        Brushes.Yellow,
        Brushes.Cyan,
        Brushes.Magenta
    };

    private DateTime _lastColorUpdate = DateTime.MinValue;
    private Color _lastAverageBackground = Colors.Black;

    public CrosshairWindow()
    {
        InitializeWindow();
        UseLayoutRounding = true;
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

            _processCheckTimer = new DispatcherTimer();
            _processCheckTimer.Interval = TimeSpan.FromMilliseconds(500);
            _processCheckTimer.Tick += CheckActiveWindow;
            _processCheckTimer.Start();

            _colorUpdateTimer = new DispatcherTimer();
            _colorUpdateTimer.Interval = TimeSpan.FromMilliseconds(20);
            _colorUpdateTimer.Tick += UpdateDynamicColor;
            _colorUpdateTimer.Start();
        };
    }

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

        _targetProcessName = ConfigUtils.ConfigRead(_activeProfile, "TargetProcess", "FortniteClient-Win64-Shipping.exe");

        var checkEnabledStr = ConfigUtils.ConfigRead(_activeProfile, "ProcessCheckEnabled", "False");
        bool.TryParse(checkEnabledStr, out _processCheckEnabled);

        var dynamicEnabledStr = ConfigUtils.ConfigRead(_activeProfile, "DynamicColorEnabled", "False");
        bool.TryParse(dynamicEnabledStr, out bool dynEnabled);
        DynamicColorEnabled = dynEnabled;

        if (DynamicColorEnabled)
        {
            var centerX = (int)SystemParameters.PrimaryScreenWidth / 2;
            var centerY = (int)SystemParameters.PrimaryScreenHeight / 2;

            var avgColor = GetAverageScreenColor(centerX, centerY, 20, 5);

            var bestBrush = GetBestContrastBrush(avgColor, CrosshairColor, true);

            CrosshairColor = bestBrush;

            _lastAverageBackground = avgColor;
            _lastColorUpdate = DateTime.Now;
        }

        UpdateCrosshairVisuals();
        CheckActiveWindow(null, null);
    }

    public void SetDynamicColorEnabled(bool enabled)
    {
        DynamicColorEnabled = enabled;
        if (!enabled)
        {
            CrosshairColor = (SolidColorBrush)new BrushConverter().ConvertFrom(ConfigUtils.ConfigRead(_activeProfile, "CrosshairColor", "#FFFF0000"));
            UpdateCrosshairVisuals();
        }
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
            catch
            {
                shouldBeVisible = false;
            }
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

    public void UpdateTargetProcess(string processName)
    {
        _targetProcessName = processName;
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
        if (!DynamicColorEnabled)
        {
            CrosshairColor = color;
        }

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
            X1 = x1 + centerX,
            Y1 = y1 + centerY,
            X2 = x2 + centerX,
            Y2 = y2 + centerY,
            Stroke = OutlineColor,
            StrokeThickness = thickness + OutlineThickness * 2,
            Opacity = CrosshairOpacity,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        var line = new Line
        {
            X1 = x1 + centerX,
            Y1 = y1 + centerY,
            X2 = x2 + centerX,
            Y2 = y2 + centerY,
            Stroke = CrosshairColor,
            StrokeThickness = thickness,
            Opacity = CrosshairOpacity,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        root.Children.Add(outline);
        root.Children.Add(line);
    }

    private void AddCircleWithOutline(Canvas root, double diameter)
    {
        var centerX = Width / 2;
        var centerY = Height / 2;

        var circle = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Stroke = CrosshairColor,
            StrokeThickness = LineThickness,
            Opacity = CrosshairOpacity
        };

        Canvas.SetLeft(circle, centerX - diameter / 2);
        Canvas.SetTop(circle, centerY - diameter / 2);

        root.Children.Add(circle);
    }

    private void AddDotWithOutline(Canvas root, double size)
    {
        var centerX = Width / 2;
        var centerY = Height / 2;

        var outline = new Ellipse
        {
            Width = size + OutlineThickness * 2,
            Height = size + OutlineThickness * 2,
            Fill = OutlineColor,
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

    private Color GetAverageScreenColor(int centerX, int centerY, int radius, int step)
    {
        int width = radius * 2;
        int height = radius * 2;
        int left = centerX - radius;
        int top = centerY - radius;

        if (left < 0 || top < 0) return Colors.Black;

        try
        {
            using (var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(left, top, 0, 0, bitmap.Size);
                }

                var rect = new System.Drawing.Rectangle(0, 0, width, height);
                var bmd = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

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

                bitmap.UnlockBits(bmd);

                if (count == 0) return Colors.Black;
                return Color.FromRgb((byte)(rSum / count), (byte)(gSum / count), (byte)(bSum / count));
            }
        }
        catch
        {
            return Colors.Black;
        }
    }

    private void UpdateDynamicColor(object sender, EventArgs e)
    {
        if (!DynamicColorEnabled || Visibility != Visibility.Visible) return;

        if ((DateTime.Now - _lastColorUpdate).TotalMilliseconds < 200) return;
        _lastColorUpdate = DateTime.Now;

        var centerX = (int)SystemParameters.PrimaryScreenWidth / 2;
        var centerY = (int)SystemParameters.PrimaryScreenHeight / 2;

        var avgColor = GetAverageScreenColor(centerX, centerY, 20, 5);

        double bgChange = GetColorDistance(avgColor, _lastAverageBackground);
        if (bgChange < 1500) return;

        _lastAverageBackground = avgColor;

        var bestBrush = GetBestContrastBrush(avgColor, CrosshairColor, false);

        if (CrosshairColor is SolidColorBrush currentSolid)
        {
            bool isValidCandidate = false;
            foreach (var candidate in _neonCandidates)
            {
                if (((SolidColorBrush)candidate).Color == currentSolid.Color)
                {
                    isValidCandidate = true;
                    break;
                }
            }

            if (!isValidCandidate)
            {
                CrosshairColor = bestBrush;
                UpdateCrosshairVisuals();
                return;
            }
        }

        SmoothColorTransition(bestBrush);
    }

    private Brush GetBestContrastBrush(Color background, Brush currentBrush, bool ignoreHysteresis = false)
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

            if (solid.Color == Colors.Red && isBrightBackground)
            {
                dist *= redBiasFactor;
            }

            if (dist > maxDiff)
            {
                maxDiff = dist;
                bestBrush = brush;
            }
        }

        if (!ignoreHysteresis && currentBrush is SolidColorBrush currentSolid)
        {
            double currentDist = GetColorDistance(currentSolid.Color, background);

            if (currentSolid.Color == Colors.Red && isBrightBackground)
            {
                currentDist *= redBiasFactor;
            }

            double threshold = currentDist * 1.25;

            if (maxDiff < threshold)
            {
                return currentBrush;
            }
        }

        return bestBrush;
    }

    private double GetColorDistance(Color c1, Color c2)
    {
        long rDiff = c1.R - c2.R;
        long gDiff = c1.G - c2.G;
        long bDiff = c1.B - c2.B;
        return (rDiff * rDiff) + (gDiff * gDiff) + (bDiff * bDiff);
    }

    private void SmoothColorTransition(Brush newBrush)
    {
        if (CrosshairColor is not SolidColorBrush currentBrush || newBrush is not SolidColorBrush targetBrush) return;

        if (currentBrush.Color == targetBrush.Color) return;

        var freshBrush = new SolidColorBrush(currentBrush.Color);
        CrosshairColor = freshBrush;

        var colorAnimation = new ColorAnimation
        {
            From = currentBrush.Color,
            To = targetBrush.Color,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            FillBehavior = FillBehavior.HoldEnd
        };

        colorAnimation.Completed += (s, e) =>
        {
            CrosshairColor = targetBrush;
            UpdateCrosshairVisuals();
        };

        freshBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        UpdateCrosshairVisuals();
    }
}