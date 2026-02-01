using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CrosshairApp.Utils;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace CrosshairApp.Windows;

public partial class SettingsWindow
{
    private readonly CrosshairWindow _mainWindow;
    private bool _suppressEvents;
    private readonly DispatcherTimer _debounceTimer;
    private string _currentProfile;

    private bool _isEditingAds;

    public SettingsWindow(CrosshairWindow mainWindow)
    {
        _suppressEvents = true;
        InitializeComponent();
        _mainWindow = mainWindow;

        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _debounceTimer.Tick += ApplyVisualSettings;

        LoadProfiles();
        LoadCurrentProfileData(ConfigUtils.GetActiveProfile());
        _suppressEvents = false;
    }

    private void LoadProfiles()
    {
        _suppressEvents = true;
        var profiles = ConfigUtils.GetProfileList();
        ProfileComboBox.ItemsSource = profiles;
        _suppressEvents = false;
    }

    private string GetKey(string baseKey)
    {
        return _isEditingAds ? "ADS_" + baseKey : baseKey;
    }

    public void LoadCurrentProfileData(string profileName)
    {
        _suppressEvents = true;
        _currentProfile = profileName;

        if (ProfileComboBox.Items.Contains(profileName))
            ProfileComboBox.SelectedItem = profileName;

        ProcessInput.Text = ConfigUtils.ConfigRead(profileName, "TargetProcess", "");
        ProcessCheck.IsChecked = bool.Parse(ConfigUtils.ConfigRead(profileName, "ProcessCheckEnabled", "False"));

        AdsEnabledCheck.IsChecked = bool.Parse(ConfigUtils.ConfigRead(profileName, "AdsEnabled", "False"));

        LoadVisualsForLayer(profileName);

        _suppressEvents = false;
        _mainWindow.SetPreviewMode(true, _isEditingAds);

        ApplyVisualSettings(null, null);
    }

    private void LoadVisualsForLayer(string profile)
    {
        StyleCombo.ItemsSource = Enum.GetValues(typeof(CrosshairStyle));
        var styleStr = ConfigUtils.ConfigRead(profile, GetKey("CrosshairStyle"), "StandardCross");
        if (Enum.TryParse(styleStr, true, out CrosshairStyle style))
            StyleCombo.SelectedItem = style;

        ColorInput.Text = ConfigUtils.ConfigRead(profile, GetKey("CrosshairColor"), "#FFFF0000");
        OutlineColorInput.Text = ConfigUtils.ConfigRead(profile, GetKey("OutlineColor"), "#FF000000");

        DynamicColorCheck.IsChecked = bool.Parse(ConfigUtils.ConfigRead(profile, GetKey("DynamicColorEnabled"), "False"));

        SetSliderSafe(SliderLenX, ConfigUtils.ConfigRead(profile, GetKey("CrosshairLength"), "8.0"));
        SetSliderSafe(SliderLenY, ConfigUtils.ConfigRead(profile, GetKey("CrosshairLengthY"), "8.0"));
        SetSliderSafe(SliderThickX, ConfigUtils.ConfigRead(profile, GetKey("LineThickness"), "3.4"));
        SetSliderSafe(SliderThickY, ConfigUtils.ConfigRead(profile, GetKey("LineThicknessY"), "3.4"));
        SetSliderSafe(SliderGap, ConfigUtils.ConfigRead(profile, GetKey("CrosshairGap"), "9.7"));
        SetSliderSafe(SliderOutline, ConfigUtils.ConfigRead(profile, GetKey("OutlineThickness"), "1.0"));
        SetSliderSafe(SliderOpacity, ConfigUtils.ConfigRead(profile, GetKey("CrosshairOpacity"), "1.0"));
        SetSliderSafe(SliderRotation, ConfigUtils.ConfigRead(profile, GetKey("RotationAngle"), "0"));
        SetSliderSafe(SliderOffsetX, ConfigUtils.ConfigRead(profile, GetKey("XOffset"), "0"));
        SetSliderSafe(SliderOffsetY, ConfigUtils.ConfigRead(profile, GetKey("YOffset"), "0"));

        UpdateColorInputsState();
        UpdateLayoutForStyle();
    }

    private void OnStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressEvents)
        {
            ApplyVisualSettings(null, null);
            UpdateLayoutForStyle();
        }
    }

    /**
     * Updates the UI to show/hide sliders irrelevant to the current style.
     * Handles specific layouts for 'Circle' and 'Dot'.
     */
    private void UpdateLayoutForStyle()
    {
        if (StyleCombo.SelectedItem is not CrosshairStyle style) return;

        bool isCircle = style == CrosshairStyle.Circle;
        bool isDot = style == CrosshairStyle.Dot;
        bool isStandard = !isCircle && !isDot;





        PanelLenY?.Visibility = isStandard ? Visibility.Visible : Visibility.Collapsed;
        PanelGap?.Visibility = isStandard ? Visibility.Visible : Visibility.Collapsed;
        PanelRotation?.Visibility = isStandard ? Visibility.Visible : Visibility.Collapsed;


        PanelThickX?.Visibility = isDot ? Visibility.Collapsed : Visibility.Visible;
        PanelThickY?.Visibility = isStandard ? Visibility.Visible : Visibility.Collapsed;


        if (LabelLenX != null)
        {
            if (isCircle) LabelLenX.Content = "Diameter:";
            else if (isDot) LabelLenX.Content = "Size:";
            else LabelLenX.Content = "Length X:";
        }

        if (LabelThickX != null && !isDot)
        {
            LabelThickX.Content = isCircle ? "Thickness:" : "Thick X:";
        }
    }

    private void SetSliderSafe(Slider slider, string value)
    {
        if (double.TryParse(value, out var result))
            slider.Value = result;
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressEvents || _debounceTimer == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnResetSetting(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string settingKey) return;

        _suppressEvents = true;

        switch (settingKey)
        {
            case "CrosshairStyle":
                StyleCombo.SelectedItem = CrosshairStyle.StandardCross;
                ConfigUtils.ConfigWrite(_currentProfile, GetKey(settingKey), "StandardCross");
                break;

            case "CrosshairColor":
                ColorInput.Text = "#FFFF0000";
                ConfigUtils.ConfigWrite(_currentProfile, GetKey(settingKey), "#FFFF0000");
                break;

            case "OutlineColor":
                OutlineColorInput.Text = "#FF000000";
                ConfigUtils.ConfigWrite(_currentProfile, GetKey(settingKey), "#FF000000");
                break;

            case "CrosshairLength":
                SliderLenX.Value = 8.0;
                break;

            case "CrosshairLengthY":
                SliderLenY.Value = 8.0;
                break;

            case "LineThickness":
                SliderThickX.Value = 3.4;
                break;

            case "LineThicknessY":
                SliderThickY.Value = 3.4;
                break;

            case "CrosshairGap":
                SliderGap.Value = 9.7;
                break;

            case "OutlineThickness":
                SliderOutline.Value = 1.0;
                break;

            case "CrosshairOpacity":
                SliderOpacity.Value = 1.0;
                break;

            case "RotationAngle":
                SliderRotation.Value = 0;
                break;

            case "XOffset":
                SliderOffsetX.Value = 0;
                break;

            case "YOffset":
                SliderOffsetY.Value = 0;
                break;
        }

        _suppressEvents = false;
        ApplyVisualSettings(null, null);
    }

    private void ApplyVisualSettings(object sender, EventArgs e)
    {
        _debounceTimer.Stop();
        try
        {
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("CrosshairLength"), SliderLenX.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("CrosshairLengthY"), SliderLenY.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("LineThickness"), SliderThickX.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("LineThicknessY"), SliderThickY.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("CrosshairGap"), SliderGap.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("OutlineThickness"), SliderOutline.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("CrosshairOpacity"), SliderOpacity.Value.ToString("F2"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("RotationAngle"), SliderRotation.Value.ToString("F0"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("XOffset"), SliderOffsetX.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, GetKey("YOffset"), SliderOffsetY.Value.ToString("F1"));

            if (StyleCombo.SelectedItem != null)
                ConfigUtils.ConfigWrite(_currentProfile, GetKey("CrosshairStyle"), StyleCombo.SelectedItem.ToString());

            _mainWindow.LoadProfile(_currentProfile);
        }
        catch { }
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents || ModeAds == null) return;

        _isEditingAds = ModeAds.IsChecked == true;

        if (!string.IsNullOrEmpty(_currentProfile))
        {
            LoadCurrentProfileData(_currentProfile);
        }
    }

    private void OnAdsToggle(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        ConfigUtils.ConfigWrite(_currentProfile, "AdsEnabled", AdsEnabledCheck.IsChecked.ToString());
        _mainWindow.LoadProfile(_currentProfile);
    }

    private Brush SafeBrushConvert(string hex, Brush fallback)
    {
        try { return (SolidColorBrush)new BrushConverter().ConvertFrom(hex); }
        catch { return fallback; }
    }

    private void OnProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || ProfileComboBox.SelectedItem == null) return;
        var newProfile = ProfileComboBox.SelectedItem.ToString();
        ConfigUtils.SetActiveProfile(newProfile);
        LoadCurrentProfileData(newProfile);
    }

    private void OnAddProfile(object sender, RoutedEventArgs e)
    {
        var inputWindow = new Window
        {
            Title = "New Profile",
            Width = 300,
            Height = 200,
            WindowStyle = WindowStyle.ToolWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = (Brush)FindResource("ControlBackground")
        };
        var stack = new StackPanel { Margin = new Thickness(10) };
        var txt = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
        var btn = new Button { Content = "Create", IsDefault = true };

        btn.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(txt.Text))
            {
                if (ConfigUtils.GetProfileList().Contains(txt.Text))
                    MessageBox.Show("Profile already exists.");
                else
                {
                    ConfigUtils.CreateProfile(txt.Text);
                    inputWindow.DialogResult = true;
                }
            }
        };

        stack.Children.Add(new Label { Content = "Profile Name:" });
        stack.Children.Add(txt);
        stack.Children.Add(btn);
        inputWindow.Content = stack;

        if (inputWindow.ShowDialog() == true)
        {
            LoadProfiles();
            ProfileComboBox.SelectedItem = txt.Text;
        }
    }

    private void OnDeleteProfile(object sender, RoutedEventArgs e)
    {
        if (_currentProfile.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Cannot delete Default profile.");
            return;
        }

        if (MessageBox.Show($"Delete '{_currentProfile}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            ConfigUtils.DeleteProfile(_currentProfile);
            LoadProfiles();
            ProfileComboBox.SelectedItem = ConfigUtils.GetActiveProfile();
        }
    }

    private void OnDynamicColorToggle(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        bool state = DynamicColorCheck.IsChecked == true;
        ConfigUtils.ConfigWrite(_currentProfile, GetKey("DynamicColorEnabled"), state.ToString());
        UpdateColorInputsState();
        ApplyVisualSettings(null, null);
    }

    private void UpdateColorInputsState()
    {
        bool locked = DynamicColorCheck.IsChecked == true;
        ColorInput.IsEnabled = !locked;
    }

    private void OnColorTextLostFocus(object sender, RoutedEventArgs e)
    {
        ConfigUtils.ConfigWrite(_currentProfile, GetKey("CrosshairColor"), ColorInput.Text);
        ApplyVisualSettings(null, null);
    }

    private void OnOutlineTextLostFocus(object sender, RoutedEventArgs e)
    {
        ConfigUtils.ConfigWrite(_currentProfile, GetKey("OutlineColor"), OutlineColorInput.Text);
        ApplyVisualSettings(null, null);
    }

    private void OnHexInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, "[0-9A-Fa-f#]");
    }

    private void OnPickColor(object sender, RoutedEventArgs e)
    {
        var dlg = new ColorDialog();
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);
            ColorInput.Text = c.ToString();
            OnColorTextLostFocus(null, null);
        }
    }

    private void OnPickOutlineColor(object sender, RoutedEventArgs e)
    {
        var dlg = new ColorDialog();
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);
            OutlineColorInput.Text = c.ToString();
            OnOutlineTextLostFocus(null, null);
        }
    }

    private void OnProcessToggle(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        bool state = ProcessCheck.IsChecked == true;
        ConfigUtils.ConfigWrite(_currentProfile, "ProcessCheckEnabled", state.ToString());
        _mainWindow.SetProcessCheckEnabled(state);
    }

    private void OnProcessTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressEvents) return;
        ConfigUtils.ConfigWrite(_currentProfile, "TargetProcess", ProcessInput.Text);
        _mainWindow.UpdateTargetProcess(ProcessInput.Text);
    }

    private void OnToggleVisibility(object sender, RoutedEventArgs e)
    {
        _mainWindow.ToggleVisibility();
    }

    private void OnSaveConfig(object sender, RoutedEventArgs e)
    {
        ConfigUtils.SaveConfig();
    }

    protected override void OnClosed(EventArgs e)
    {
        ConfigUtils.SaveConfig();
        _mainWindow.SetPreviewMode(false, false);
        base.OnClosed(e);
    }
}