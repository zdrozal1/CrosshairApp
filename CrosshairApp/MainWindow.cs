using System;
using System.Windows.Media;
using CrosshairApp.Utils;
using CrosshairApp.Windows;

namespace CrosshairApp;

public partial class MainWindow
{
    private readonly CrosshairWindow _mainWindow;

    public MainWindow(CrosshairWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        Width = 385;
        Height = 533;
        Topmost = true;

        xOffsetSlider.Minimum = -30;
        xOffsetSlider.Maximum = 30;
        xOffsetSlider.Value = mainWindow.XOffset;
        xOffsetSlider.SmallChange = 0.05;
        xOffsetSlider.LargeChange = 0.1;
        xOffsetSlider.TickFrequency = 0.05;
        xOffsetSlider.IsSnapToTickEnabled = true;

        yOffsetSlider.Minimum = -30;
        yOffsetSlider.Maximum = 30;
        yOffsetSlider.Value = mainWindow.YOffset;
        yOffsetSlider.SmallChange = 0.05;
        yOffsetSlider.LargeChange = 0.1;
        yOffsetSlider.TickFrequency = 0.05;
        yOffsetSlider.IsSnapToTickEnabled = true;

        lengthSlider.Minimum = 0;
        lengthSlider.Maximum = 30;
        lengthSlider.Value = mainWindow.CrosshairLength;
        lengthSlider.SmallChange = 0.05;
        lengthSlider.LargeChange = 0.1;
        lengthSlider.TickFrequency = 0.05;
        lengthSlider.IsSnapToTickEnabled = true;

        thicknessSlider.Minimum = 0;
        thicknessSlider.Maximum = 5;
        thicknessSlider.Value = mainWindow.LineThickness;
        thicknessSlider.SmallChange = 0.01;
        thicknessSlider.LargeChange = 0.05;
        thicknessSlider.TickFrequency = 0.01;
        thicknessSlider.IsSnapToTickEnabled = true;

        gapSlider.Minimum = 0;
        gapSlider.Maximum = 30;
        gapSlider.Value = mainWindow.CrosshairGap;
        gapSlider.SmallChange = 0.05;
        gapSlider.LargeChange = 0.1;
        gapSlider.TickFrequency = 0.05;
        gapSlider.IsSnapToTickEnabled = true;

        rotationSlider.Minimum = 0;
        rotationSlider.Maximum = 360;
        rotationSlider.Value = mainWindow.RotationAngle;
        rotationSlider.SmallChange = 0.5;
        rotationSlider.LargeChange = 1.0;
        rotationSlider.TickFrequency = 1;
        rotationSlider.IsSnapToTickEnabled = true;

        opacitySlider.Minimum = 0;
        opacitySlider.Maximum = 1;
        opacitySlider.Value = mainWindow.CrosshairOpacity;
        opacitySlider.SmallChange = 0.05;
        opacitySlider.LargeChange = 0.1;
        opacitySlider.TickFrequency = 0.05;
        opacitySlider.IsSnapToTickEnabled = true;

        colorComboBox.ItemsSource = Enum.GetValues(typeof(CrosshairColor));
        colorComboBox.SelectedValue = GetColorForBrush(mainWindow.CrosshairColor);

        styleComboBox.ItemsSource = Enum.GetValues(typeof(CrosshairStyle));
        styleComboBox.SelectedValue = mainWindow.CrosshairStyle;

        lengthLabel.Content = "Crosshair Length: " + Math.Round(mainWindow.CrosshairLength, 2);
        thicknessLabel.Content = "Line Thickness: " + Math.Round(mainWindow.LineThickness, 2);
        gapLabel.Content = "Crosshair Gap: " + Math.Round(mainWindow.CrosshairGap, 2);
        rotationLabel.Content = "Crosshair Rotation: " + Math.Round(mainWindow.RotationAngle, 2);
        opacityLabel.Content = "Opacity: " + Math.Round(mainWindow.CrosshairOpacity, 2);
        xOffsetLabel.Content = "XOffset: " + Math.Round(mainWindow.XOffset, 2);
        yOffsetLabel.Content = "YOffset: " + Math.Round(mainWindow.YOffset, 2);

        lengthSlider.ValueChanged += (_, _) =>
        {
            lengthLabel.Content = "Crosshair Length: " + Math.Round(lengthSlider.Value, 2);
            ConfigUtils.ConfigWrite("CrosshairLength", lengthSlider.Value.ToString());
            UpdateCrosshair();
        };
        thicknessSlider.ValueChanged += (_, _) =>
        {
            thicknessLabel.Content = "Line Thickness: " + Math.Round(thicknessSlider.Value, 2);
            ConfigUtils.ConfigWrite("LineThickness", thicknessSlider.Value.ToString());
            UpdateCrosshair();
        };
        gapSlider.ValueChanged += (_, _) =>
        {
            gapLabel.Content = "Crosshair Gap: " + Math.Round(gapSlider.Value, 2);
            ConfigUtils.ConfigWrite("CrosshairGap", gapSlider.Value.ToString());
            UpdateCrosshair();
        };
        rotationSlider.ValueChanged += (_, _) =>
        {
            rotationLabel.Content = "Crosshair Rotation: " + Math.Round(rotationSlider.Value, 2);
            ConfigUtils.ConfigWrite("RotationAngle", rotationSlider.Value.ToString());
            UpdateCrosshair();
        };
        opacitySlider.ValueChanged += (_, _) =>
        {
            opacityLabel.Content = "Opacity: " + Math.Round(opacitySlider.Value, 2);
            ConfigUtils.ConfigWrite("CrosshairOpacity", opacitySlider.Value.ToString());
            UpdateCrosshair();
        };
        styleComboBox.SelectionChanged += (_, _) =>
        {
            ConfigUtils.ConfigWrite("CrosshairStyle", styleComboBox.SelectedValue.ToString());
            UpdateCrosshair();
        };
        colorComboBox.SelectionChanged += (_, _) =>
        {
            ConfigUtils.ConfigWrite("CrosshairColor", colorComboBox.SelectedValue.ToString());
            UpdateCrosshair();
        };
        xOffsetSlider.ValueChanged += (_, _) =>
        {
            xOffsetLabel.Content = "XOffset: " + Math.Round(xOffsetSlider.Value, 2);
            ConfigUtils.ConfigWrite("XOffset", xOffsetSlider.Value.ToString());
            mainWindow.UpdateWindowPosition(xOffsetSlider.Value, yOffsetSlider.Value);
        };
        yOffsetSlider.ValueChanged += (_, _) =>
        {
            yOffsetLabel.Content = "YOffset: " + Math.Round(yOffsetSlider.Value, 2);
            ConfigUtils.ConfigWrite("YOffset", yOffsetSlider.Value.ToString());
            mainWindow.UpdateWindowPosition(xOffsetSlider.Value, yOffsetSlider.Value);
        };

        UpdateCrosshair();
    }

    private void UpdateCrosshair()
    {
        var length = lengthSlider.Value;
        var thickness = thicknessSlider.Value;
        var gap = gapSlider.Value;
        var opacity = opacitySlider.Value;
        var style = (CrosshairStyle)styleComboBox.SelectedItem;
        var color = GetBrushForColor((CrosshairColor)colorComboBox.SelectedItem);
        var rotation = rotationSlider.Value;
        var xOffset = xOffsetSlider.Value;
        var yOffset = yOffsetSlider.Value;

        _mainWindow.UpdateCrosshairProperties(length, thickness, gap, opacity, style, color, rotation, xOffset,
            yOffset);
    }

    private CrosshairColor GetColorForBrush(Brush brush)
    {
        if (brush == Brushes.Red)
            return CrosshairColor.Red;
        if (brush == Brushes.Green)
            return CrosshairColor.Green;
        if (brush == Brushes.Blue)
            return CrosshairColor.Blue;
        if (brush == Brushes.Yellow)
            return CrosshairColor.Yellow;
        if (brush == Brushes.White)
            return CrosshairColor.White;
        if (brush == Brushes.Purple)
            return CrosshairColor.Purple;
        if (brush == Brushes.Cyan)
            return CrosshairColor.Cyan;
        if (brush == Brushes.Magenta)
            return CrosshairColor.Magenta;

        return CrosshairColor.Red;
    }

    private Brush GetBrushForColor(CrosshairColor color)
    {
        return color switch
        {
            CrosshairColor.Red => Brushes.Red,
            CrosshairColor.Green => Brushes.Green,
            CrosshairColor.Blue => Brushes.Blue,
            CrosshairColor.Yellow => Brushes.Yellow,
            CrosshairColor.White => Brushes.White,
            CrosshairColor.Purple => Brushes.Purple,
            CrosshairColor.Cyan => Brushes.Cyan,
            CrosshairColor.Magenta => Brushes.Magenta,
            _ => Brushes.Red
        };
    }
}