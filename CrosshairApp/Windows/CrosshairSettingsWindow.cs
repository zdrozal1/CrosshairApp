using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CrosshairApp.Utils;

namespace CrosshairApp.Windows;

public class CrosshairSettingsWindow : Window
{
    private readonly ComboBox _colorComboBox;
    private readonly Slider _gapSlider;
    private readonly Slider _lengthSlider;
    private readonly CrosshairWindow _mainWindow;
    private readonly Slider _opacitySlider;
    private readonly Slider _rotationSlider;
    private readonly ComboBox _styleComboBox;
    private readonly Slider _thicknessSlider;
    private readonly Slider _xOffsetSlider;
    private readonly Slider _yOffsetSlider;

    public CrosshairSettingsWindow(CrosshairWindow mainWindow)
    {
        _mainWindow = mainWindow;
        Title = "Crosshair Settings";
        Width = 345;
        Height = 460;

        var grid = new Grid
        {
            Margin = new Thickness(6)
        };
        Content = grid;
        Topmost = true;

        _xOffsetSlider = new Slider
        {
            Minimum = -30,
            Maximum = 30,
            Value = mainWindow.XOffset,
            SmallChange = 0.05,
            LargeChange = 0.1,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = true
        };
        _yOffsetSlider = new Slider
        {
            Minimum = -30,
            Maximum = 30,
            Value = mainWindow.YOffset,
            SmallChange = 0.05,
            LargeChange = 0.1,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = true
        };
        _lengthSlider = new Slider
        {
            Minimum = 0, Maximum = 30, Value = mainWindow.CrosshairLength,
            SmallChange = 0.05,
            LargeChange = 0.1,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = true
        };
        _thicknessSlider = new Slider
        {
            Minimum = 0, Maximum = 5, Value = mainWindow.LineThickness,
            SmallChange = 0.01,
            LargeChange = 0.05,
            TickFrequency = 0.01,
            IsSnapToTickEnabled = true
        };
        _gapSlider = new Slider
        {
            Minimum = 0, Maximum = 30, Value = mainWindow.CrosshairGap,
            SmallChange = 0.05,
            LargeChange = 0.1,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = true
        };
        _rotationSlider = new Slider
        {
            Minimum = 0, Maximum = 360, Value = mainWindow.RotationAngle,
            SmallChange = 0.5,
            LargeChange = 1.0,
            TickFrequency = 1,
            IsSnapToTickEnabled = true
        };
        _opacitySlider = new Slider
        {
            Minimum = 0, Maximum = 1, Value = mainWindow.Opacity,
            SmallChange = 0.05,
            LargeChange = 0.1,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = true
        };
        _styleComboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(CrosshairStyle)),
            SelectedValue = mainWindow.CrosshairStyle
        };
        _colorComboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(CrosshairColor)),
            SelectedValue = GetColorForBrush(mainWindow.CrosshairColor)
        };

        for (var i = 0; i < 18; i++) grid.RowDefinitions.Add(new RowDefinition());

        var lengthLabel = new TextBlock { Text = "Crosshair Length: " + Math.Round(mainWindow.CrosshairLength, 2) };
        var thicknessLabel = new TextBlock { Text = "Line Thickness: " + Math.Round(mainWindow.LineThickness, 2) };
        var gapLabel = new TextBlock { Text = "Crosshair Gap: " + Math.Round(mainWindow.CrosshairGap, 2) };
        var rotationLabel = new TextBlock { Text = "Crosshair Rotation: " + Math.Round(mainWindow.RotationAngle, 2) };
        var opacityLabel = new TextBlock { Text = "Opacity: " + Math.Round(mainWindow.CrosshairOpacity, 2) };
        var styleLabel = new TextBlock { Text = "Crosshair Style: " };
        var colorLabel = new TextBlock { Text = "Crosshair Color: " };

        var xoffsetlabel = new TextBlock { Text = "XOffset: " + Math.Round(mainWindow.XOffset, 2) };
        var yoffsetlabel = new TextBlock { Text = "YOffset: " + Math.Round(mainWindow.YOffset, 2) };

        grid.Children.Add(lengthLabel);
        grid.Children.Add(thicknessLabel);
        grid.Children.Add(gapLabel);
        grid.Children.Add(rotationLabel);
        grid.Children.Add(opacityLabel);
        grid.Children.Add(xoffsetlabel);
        grid.Children.Add(yoffsetlabel);
        grid.Children.Add(styleLabel);
        grid.Children.Add(colorLabel);
        grid.Children.Add(_lengthSlider);
        grid.Children.Add(_thicknessSlider);
        grid.Children.Add(_gapSlider);
        grid.Children.Add(_rotationSlider);
        grid.Children.Add(_opacitySlider);
        grid.Children.Add(_xOffsetSlider);
        grid.Children.Add(_yOffsetSlider);
        grid.Children.Add(_styleComboBox);
        grid.Children.Add(_colorComboBox);
        Grid.SetRow(lengthLabel, 0);
        Grid.SetRow(_lengthSlider, 1);
        Grid.SetRow(thicknessLabel, 2);
        Grid.SetRow(_thicknessSlider, 3);
        Grid.SetRow(gapLabel, 4);
        Grid.SetRow(_gapSlider, 5);
        Grid.SetRow(rotationLabel, 6);
        Grid.SetRow(_rotationSlider, 7);
        Grid.SetRow(opacityLabel, 8);
        Grid.SetRow(_opacitySlider, 9);
        Grid.SetRow(xoffsetlabel, 10);
        Grid.SetRow(_xOffsetSlider, 11);
        Grid.SetRow(yoffsetlabel, 12);
        Grid.SetRow(_yOffsetSlider, 13);
        Grid.SetRow(styleLabel, 14);
        Grid.SetRow(_styleComboBox, 15);
        Grid.SetRow(colorLabel, 16);
        Grid.SetRow(_colorComboBox, 17);

        _lengthSlider.ValueChanged += (_, _) =>
        {
            lengthLabel.Text = "Crosshair Length: " + Math.Round(_lengthSlider.Value, 2);
            ConfigUtils.ConfigWrite("CrosshairLength", _lengthSlider.Value.ToString());
            UpdateCrosshair();
        };
        _thicknessSlider.ValueChanged += (_, _) =>
        {
            thicknessLabel.Text = "Line Thickness: " + Math.Round(_thicknessSlider.Value, 2);
            ConfigUtils.ConfigWrite("LineThickness", _thicknessSlider.Value.ToString());
            UpdateCrosshair();
        };
        _gapSlider.ValueChanged += (_, _) =>
        {
            gapLabel.Text = "Crosshair Gap: " + Math.Round(_gapSlider.Value, 2);
            ConfigUtils.ConfigWrite("CrosshairGap", _gapSlider.Value.ToString());
            UpdateCrosshair();
        };
        _rotationSlider.ValueChanged += (_, _) =>
        {
            rotationLabel.Text = "Crosshair Rotation: " + Math.Round(_rotationSlider.Value, 2);
            ConfigUtils.ConfigWrite("RotationAngle", _rotationSlider.Value.ToString());
            UpdateCrosshair();
        };
        _opacitySlider.ValueChanged += (_, _) =>
        {
            opacityLabel.Text = "Opacity: " + Math.Round(_opacitySlider.Value, 2);
            ConfigUtils.ConfigWrite("CrosshairOpacity", _opacitySlider.Value.ToString());
            UpdateCrosshair();
        };
        _styleComboBox.SelectionChanged += (_, _) =>
        {
            ConfigUtils.ConfigWrite("CrosshairStyle", _styleComboBox.SelectedValue.ToString());
            UpdateCrosshair();
        };
        _colorComboBox.SelectionChanged += (_, _) =>
        {
            ConfigUtils.ConfigWrite("CrosshairColor", _colorComboBox.SelectedValue.ToString());
            UpdateCrosshair();
        };
        _xOffsetSlider.ValueChanged += (_, _) =>
        {
            xoffsetlabel.Text = "XOffset: " + Math.Round(_xOffsetSlider.Value, 2);
            ConfigUtils.ConfigWrite("XOffset", _xOffsetSlider.Value.ToString());
            mainWindow.UpdateWindowPosition(_xOffsetSlider.Value, _yOffsetSlider.Value);
        };
        _yOffsetSlider.ValueChanged += (_, _) =>
        {
            yoffsetlabel.Text = "YOffset: " + Math.Round(_yOffsetSlider.Value, 2);
            ConfigUtils.ConfigWrite("YOffset", _yOffsetSlider.Value.ToString());
            mainWindow.UpdateWindowPosition(_xOffsetSlider.Value, _yOffsetSlider.Value);
        };
    }

    private void UpdateCrosshair()
    {
        var length = _lengthSlider.Value;
        var thickness = _thicknessSlider.Value;
        var gap = _gapSlider.Value;
        var opacity = _opacitySlider.Value;
        var style = (CrosshairStyle)_styleComboBox.SelectedItem;
        var color = GetBrushForColor((CrosshairColor)_colorComboBox.SelectedItem);
        var rotation = _rotationSlider.Value;
        var xOffset = _xOffsetSlider.Value;
        var yOffset = _yOffsetSlider.Value;

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