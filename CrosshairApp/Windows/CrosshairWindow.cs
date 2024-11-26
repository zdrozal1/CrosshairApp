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
    private NotifyIcon _notifyIcon;

    public Brush CrosshairColor =
        (Brush)typeof(Brushes).GetProperty(ConfigUtils.ConfigRead("CrosshairColor"))?.GetValue(null);

    public double CrosshairGap = Convert.ToDouble(ConfigUtils.ConfigRead("CrosshairGap"));
    public double CrosshairLength = Convert.ToDouble(ConfigUtils.ConfigRead("CrosshairLength"));
    public double CrosshairOpacity = Convert.ToDouble(ConfigUtils.ConfigRead("CrosshairOpacity"));

    public CrosshairStyle CrosshairStyle =
        (CrosshairStyle)Enum.Parse(typeof(CrosshairStyle), ConfigUtils.ConfigRead("CrosshairStyle"));

    public double LineThickness = Convert.ToDouble(ConfigUtils.ConfigRead("LineThickness"));
    public double RotationAngle = Convert.ToDouble(ConfigUtils.ConfigRead("RotationAngle"));

    public double XOffset = Convert.ToDouble(ConfigUtils.ConfigRead("XOffset"));
    public double YOffset = Convert.ToDouble(ConfigUtils.ConfigRead("YOffset"));

    public CrosshairWindow()
    {
        InitializeWindow();
        CreateTrayIcon();

        _root = new Canvas { IsHitTestVisible = false };
        AddCrosshair(_root);

        Content = _root;

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        };

        Closing += (_, e) =>
        {
            e.Cancel = true;
            HideWindow();
        };
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
        Width = CrosshairLength * 4;
        Height = CrosshairLength * 4;
        WindowStyle = WindowStyle.None;
        Topmost = true;
        AllowsTransparency = true;
        ShowInTaskbar = false;

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2 + XOffset;
        Top = (SystemParameters.PrimaryScreenHeight - Height) / 2 + YOffset;
    }

    public void UpdateWindowPosition(double xOffset, double yOffset)
    {
        XOffset = xOffset;
        YOffset = yOffset;

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2 + XOffset;
        Top = (SystemParameters.PrimaryScreenHeight - Height) / 2 + YOffset;
    }

    private void CreateTrayIcon()
    {
        var resourceName = "CrosshairApp.Images.target.ico";
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = new Icon(stream),
                    Visible = true,
                    Text = TrayIconText,
                    ContextMenu = new ContextMenu([
                        new MenuItem("Settings", (_, _) => ShowWindow()),
                        new MenuItem("Exit", (_, _) => ExitApplication())
                    ])
                };

                _notifyIcon.DoubleClick += (_, _) => ShowWindow();
            }
        }
    }

    private void ShowWindow()
    {
        var settingsWindow = new MainWindow(this);
        settingsWindow.ShowDialog();
    }

    private void HideWindow()
    {
        Hide();
        _notifyIcon.Visible = true;
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
        root.Children.Add(CreateLine(0, -CrosshairLength - CrosshairGap / 2, 0, -CrosshairGap / 2));
        root.Children.Add(CreateLine(0, CrosshairGap / 2, 0, CrosshairLength + CrosshairGap / 2));
        root.Children.Add(CreateLine(-CrosshairLength - CrosshairGap / 2, 0, -CrosshairGap / 2, 0));
        root.Children.Add(CreateLine(CrosshairGap / 2, 0, CrosshairLength + CrosshairGap / 2, 0));
    }

    private void AddCircleCrosshair(Canvas root)
    {
        root.Children.Add(CreateCircle(CrosshairLength * 2, CrosshairLength * 2));
    }

    private void AddDotCrosshair(Canvas root)
    {
        root.Children.Add(CreateDot(CrosshairLength / 2));
    }

    public void UpdateCrosshairProperties(double length, double thickness, double gap, double opacity,
        CrosshairStyle style, Brush color, double rotationAngle, double xOffset, double yOffset)
    {
        CrosshairLength = length;
        LineThickness = thickness;
        CrosshairGap = gap;
        CrosshairOpacity = opacity;
        CrosshairStyle = style;
        CrosshairColor = color;
        RotationAngle = rotationAngle;
        XOffset = xOffset;
        YOffset = yOffset;

        Width = CrosshairLength * 4;
        Height = CrosshairLength * 4;

        Left = (SystemParameters.PrimaryScreenWidth - Width) / 2 + XOffset;
        Top = (SystemParameters.PrimaryScreenHeight - Height) / 2 + YOffset;

        _root.Children.Clear();
        AddCrosshair(_root);
    }

    private void AddCircleWithXCrosshair(Canvas root)
    {
        AddCircleCrosshair(root);

        var adjustedLength = CrosshairLength - CrosshairGap;
        root.Children.Add(CreateLine(-adjustedLength, -adjustedLength, adjustedLength, adjustedLength));
        root.Children.Add(CreateLine(adjustedLength, -adjustedLength, -adjustedLength, adjustedLength));
    }

    private Line CreateLine(double x1, double y1, double x2, double y2)
    {
        return new Line
        {
            X1 = x1 + CrosshairLength * 2,
            Y1 = y1 + CrosshairLength * 2,
            X2 = x2 + CrosshairLength * 2,
            Y2 = y2 + CrosshairLength * 2,
            Stroke = CrosshairColor,
            StrokeThickness = LineThickness,
            Opacity = CrosshairOpacity
        };
    }

    private Ellipse CreateCircle(double diameter, double offset)
    {
        var circle = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Stroke = CrosshairColor,
            StrokeThickness = LineThickness,
            Opacity = CrosshairOpacity
        };

        SetPosition(circle, offset - diameter / 2, offset - diameter / 2);
        return circle;
    }

    private Ellipse CreateDot(double size)
    {
        var dot = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = CrosshairColor,
            Opacity = CrosshairOpacity
        };

        SetPosition(dot, CrosshairLength * 2 - size / 2, CrosshairLength * 2 - size / 2);
        return dot;
    }

    private void SetPosition(UIElement element, double left, double top)
    {
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
    }

    private void ApplyRotation(Canvas root)
    {
        var rotateTransform =
            new RotateTransform(RotationAngle, CrosshairLength * 2, CrosshairLength * 2);
        root.RenderTransform = rotateTransform;
    }
}