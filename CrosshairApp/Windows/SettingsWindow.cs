using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CrosshairApp.Utils;

namespace CrosshairApp.Windows;

public partial class SettingsWindow
{
    private readonly CrosshairWindow _mainWindow;

    public SettingsWindow(CrosshairWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        Width = 385;
        Height = 533;
        Topmost = true;

        InitializeSlider(xOffsetSlider, -30, 30, mainWindow.XOffset);
        InitializeSlider(yOffsetSlider, -30, 30, mainWindow.YOffset);
        InitializeSlider(lengthSlider, 0, 30, mainWindow.CrosshairLength);
        InitializeSlider(thicknessSlider, 0, 5, mainWindow.LineThickness);
        InitializeSlider(gapSlider, 0, 30, mainWindow.CrosshairGap);
        InitializeSlider(rotationSlider, 0, 360, mainWindow.RotationAngle);
        InitializeSlider(opacitySlider, 0, 1, mainWindow.CrosshairOpacity);

        colorComboBox.ItemsSource = Enum.GetValues(typeof(CrosshairColor));
        colorComboBox.SelectedValue = GetColorForBrush(mainWindow.CrosshairColor);

        styleComboBox.ItemsSource = Enum.GetValues(typeof(CrosshairStyle));
        styleComboBox.SelectedValue = mainWindow.CrosshairStyle;

        UpdateLabels(mainWindow);
        InitializeEventHandlers();

        UpdateCrosshair();
    }

    private void InitializeSlider(Slider slider, double min, double max, double value)
    {
        slider.Minimum = min;
        slider.Maximum = max;
        slider.Value = value;
        slider.SmallChange = 0.05;
        slider.LargeChange = 0.1;
        slider.TickFrequency = 0.05;
        slider.IsSnapToTickEnabled = true;
    }

    private void UpdateLabels(CrosshairWindow mainWindow)
    {
        lengthLabel.Content = $"Crosshair Length: {Math.Round(mainWindow.CrosshairLength, 2)}";
        thicknessLabel.Content = $"Line Thickness: {Math.Round(mainWindow.LineThickness, 2)}";
        gapLabel.Content = $"Crosshair Gap: {Math.Round(mainWindow.CrosshairGap, 2)}";
        rotationLabel.Content = $"Crosshair Rotation: {Math.Round(mainWindow.RotationAngle, 2)}";
        opacityLabel.Content = $"Opacity: {Math.Round(mainWindow.CrosshairOpacity, 2)}";
        xOffsetLabel.Content = $"XOffset: {Math.Round(mainWindow.XOffset, 2)}";
        yOffsetLabel.Content = $"YOffset: {Math.Round(mainWindow.YOffset, 2)}";
    }

    private void InitializeEventHandlers()
    {
        lengthSlider.ValueChanged += SliderValueChanged;
        thicknessSlider.ValueChanged += SliderValueChanged;
        gapSlider.ValueChanged += SliderValueChanged;
        rotationSlider.ValueChanged += SliderValueChanged;
        opacitySlider.ValueChanged += SliderValueChanged;
        styleComboBox.SelectionChanged += ComboBoxSelectionChanged;
        colorComboBox.SelectionChanged += ComboBoxSelectionChanged;
        xOffsetSlider.ValueChanged += SliderValueChanged;
        yOffsetSlider.ValueChanged += SliderValueChanged;
    }

    private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (Equals(sender, lengthSlider))
        {
            lengthLabel.Content = $"Crosshair Length: {Math.Round(lengthSlider.Value, 2)}";
            ConfigUtils.ConfigWrite("CrosshairLength", lengthSlider.Value.ToString());
        }
        else if (Equals(sender, thicknessSlider))
        {
            thicknessLabel.Content = $"Line Thickness: {Math.Round(thicknessSlider.Value, 2)}";
            ConfigUtils.ConfigWrite("LineThickness", thicknessSlider.Value.ToString());
        }
        else if (Equals(sender, gapSlider))
        {
            gapLabel.Content = $"Crosshair Gap: {Math.Round(gapSlider.Value, 2)}";
            ConfigUtils.ConfigWrite("CrosshairGap", gapSlider.Value.ToString());
        }
        else if (Equals(sender, rotationSlider))
        {
            rotationLabel.Content = $"Crosshair Rotation: {Math.Round(rotationSlider.Value, 2)}";
            ConfigUtils.ConfigWrite("RotationAngle", rotationSlider.Value.ToString());
        }
        else if (Equals(sender, opacitySlider))
        {
            opacityLabel.Content = $"Opacity: {Math.Round(opacitySlider.Value, 2)}";
            ConfigUtils.ConfigWrite("CrosshairOpacity", opacitySlider.Value.ToString());
        }
        else if (Equals(sender, xOffsetSlider))
        {
            xOffsetLabel.Content = $"XOffset: {Math.Round(xOffsetSlider.Value, 2)}";
            ConfigUtils.ConfigWrite("XOffset", xOffsetSlider.Value.ToString());
            _mainWindow.UpdateWindowPosition(xOffsetSlider.Value, yOffsetSlider.Value);
        }
        else if (Equals(sender, yOffsetSlider))
        {
            yOffsetLabel.Content = $"YOffset: {Math.Round(yOffsetSlider.Value, 2)}";
            ConfigUtils.ConfigWrite("YOffset", yOffsetSlider.Value.ToString());
            _mainWindow.UpdateWindowPosition(xOffsetSlider.Value, yOffsetSlider.Value);
        }

        UpdateCrosshair();
    }

    private void ComboBoxSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (sender == styleComboBox)
            ConfigUtils.ConfigWrite("CrosshairStyle", styleComboBox.SelectedValue.ToString());
        else if (sender == colorComboBox)
            ConfigUtils.ConfigWrite("CrosshairColor", colorComboBox.SelectedValue.ToString());

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

    private void Reset_Def(object sender, RoutedEventArgs e)
    {
        lengthSlider.Value = 8;
        thicknessSlider.Value = 3.4;
        gapSlider.Value = 9.7;
        rotationSlider.Value = 0;
        opacitySlider.Value = 1.0;
        xOffsetSlider.Value = 0;
        yOffsetSlider.Value = 0;

        colorComboBox.SelectedValue = CrosshairColor.Red;
        styleComboBox.SelectedValue = CrosshairStyle.StandardCross;

        UpdateLabels(_mainWindow);

        UpdateCrosshair();
    }
}