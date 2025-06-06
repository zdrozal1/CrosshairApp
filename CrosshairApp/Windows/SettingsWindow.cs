using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using CrosshairApp.Utils;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace CrosshairApp.Windows
{
    public partial class SettingsWindow
    {
        private readonly CrosshairWindow _mainWindow;
        private string _currentProfile;
        private bool _isInternalUpdate;

        public SettingsWindow(CrosshairWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            LoadProfileList();
            LoadProfileSettings(ConfigUtils.GetActiveProfile());

            versionLabel.Text = $"Version {Launcher.Version}";
            ToggleCrosshairButton.Content =
                _mainWindow.Visibility == Visibility.Visible ? "Hide Crosshair" : "Show Crosshair";
        }

        private void InitializeSlider(Slider slider, double min, double max, double value, double smallChange = 0.1,
            double largeChange = 0.5)
        {
            slider.Minimum = min;
            slider.Maximum = max;
            slider.Value = value;
            slider.SmallChange = smallChange;
            slider.LargeChange = largeChange;
            slider.TickFrequency = smallChange;
            slider.IsSnapToTickEnabled = true;
        }

        private void LoadProfileList()
        {
            _isInternalUpdate = true;
            var profiles = ConfigUtils.GetProfileList();
            var activeProfile = ConfigUtils.GetActiveProfile();
            ProfileComboBox.ItemsSource = profiles;
            ProfileComboBox.SelectedItem = activeProfile;
            _currentProfile = activeProfile;

            AdsProfileComboBox.ItemsSource = profiles;

            var adsProfile = ConfigUtils.GetAdsProfile();
            if (profiles.Contains(adsProfile))
            {
                AdsProfileComboBox.SelectedItem = adsProfile;
            }
            else if (profiles.Any())
            {
                AdsProfileComboBox.SelectedIndex = 0;
                ConfigUtils.SetAdsProfile(profiles.First());
            }

            _isInternalUpdate = false;
        }

        public void LoadProfileSettings(string profileName)
        {
            _isInternalUpdate = true;
            _currentProfile = profileName;

            if (ProfileComboBox.SelectedItem?.ToString() != _currentProfile)
                ProfileComboBox.SelectedItem = _currentProfile;

            StyleComboBox.ItemsSource = Enum.GetValues(typeof(CrosshairStyle));
            var styleValue = ConfigUtils.ConfigRead(_currentProfile, "CrosshairStyle");
            if (Enum.TryParse(styleValue, true, out CrosshairStyle parsedStyle))
                StyleComboBox.SelectedItem = parsedStyle;

            ColorTextBox.Text = ConfigUtils.ConfigRead(_currentProfile, "CrosshairColor", "#FFFF0000");
            OutlineColorTextBox.Text = ConfigUtils.ConfigRead(_currentProfile, "OutlineColor", "#FF000000");

            InitializeSlider(LengthSlider, 0, 50,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "CrosshairLength", "8.0")));

            InitializeSlider(LengthYSlider, 0, 50,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "CrosshairLengthY", "8.0")));

            InitializeSlider(ThicknessSlider, 0.1, 25,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "LineThickness", "3.4")));

            InitializeSlider(ThicknessYSlider, 0.1, 25,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "LineThicknessY", "3.4")));

            InitializeSlider(GapSlider, 0, 50,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "CrosshairGap", "9.7")));

            InitializeSlider(RotationSlider, 0, 360,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "RotationAngle", "0")));

            InitializeSlider(OpacitySlider, 0, 1,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "CrosshairOpacity", "1.0")));

            InitializeSlider(OutlineThicknessSlider, 0, 10,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "OutlineThickness", "1.0")));

            InitializeSlider(XOffsetSlider, -100, 100,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "XOffset", "0")));

            InitializeSlider(YOffsetSlider, -100, 100,
                Convert.ToDouble(ConfigUtils.ConfigRead(_currentProfile, "YOffset", "0")));

            EnableAdsProfileCheckBox.IsChecked = ConfigUtils.GetEnableAdsProfile();

            UpdateProfileButtonVisibility();
            UpdateLabels();
            _isInternalUpdate = false;
        }

        private void UpdateLabels()
        {
            LengthLabel.Content = $"Length (Horizontal): {LengthSlider.Value:F1}";
            LengthYLabel.Content = $"Length (Vertical): {LengthYSlider.Value:F1}";
            ThicknessLabel.Content = $"Thickness (Horizontal): {ThicknessSlider.Value:F1}";
            ThicknessYLabel.Content = $"Thickness (Vertical): {ThicknessYSlider.Value:F1}";
            GapLabel.Content = $"Gap: {GapSlider.Value:F1}";
            RotationLabel.Content = $"Rotation: {RotationSlider.Value:F0}°";
            OpacityLabel.Content = $"Opacity: {OpacitySlider.Value:F2}";
            OutlineThicknessLabel.Content = $"Outline Thickness: {OutlineThicknessSlider.Value:F1}";
            XOffsetLabel.Content = $"X Offset: {XOffsetSlider.Value:F1}";
            YOffsetLabel.Content = $"Y Offset: {YOffsetSlider.Value:F1}";
        }

        private void UpdateProfileButtonVisibility()
        {
            if (string.IsNullOrEmpty(_currentProfile)) return;

            var isDefaultProfile = _currentProfile.Equals("Default", StringComparison.OrdinalIgnoreCase);

            ResetProfileButton.Visibility = isDefaultProfile ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnSettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInternalUpdate) return;
            UpdateLabels();
            SaveChangesToProfile();
            UpdateCrosshair();
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInternalUpdate || ProfileComboBox.SelectedItem == null) return;

            var selectedProfile = ProfileComboBox.SelectedItem.ToString();

            _mainWindow.SwitchBaseProfile(selectedProfile);

            LoadProfileSettings(selectedProfile);
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Create Profile", Width = 300, Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var textBlock = new TextBlock { Text = "Enter new profile name:", Margin = new Thickness(0, 0, 0, 5) };
            var textBox = new TextBox { Name = "ProfileNameTextBox" };
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var okButton = new Button { Content = "OK", IsDefault = true, Width = 75, Margin = new Thickness(5) };
            var cancelButton = new Button
                { Content = "Cancel", IsCancel = true, Width = 75, Margin = new Thickness(5) };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);
            dialog.Content = stackPanel;
            okButton.Click += (s, args) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };
            cancelButton.Click += (s, args) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };
            textBox.Focus();

            if (dialog.ShowDialog() != true) return;
            var newProfileName = textBox.Text;
            if (string.IsNullOrWhiteSpace(newProfileName)) return;
            if (!ConfigUtils.GetProfileList().Any(p => p.Equals(newProfileName, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigUtils.CreateProfile(newProfileName);
                LoadProfileList();
                ProfileComboBox.SelectedItem = newProfileName;
            }
            else
            {
                MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileToDelete = ProfileComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(profileToDelete) ||
                profileToDelete.Equals("Default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The Default profile cannot be deleted.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete the '{profileToDelete}' profile?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            ConfigUtils.DeleteProfile(profileToDelete);
            LoadProfileList();
            LoadProfileSettings(_currentProfile);
        }

        private void ResetProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset the Default profile to its original settings?",
                "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            ConfigUtils.DeleteProfile("Default");

            ConfigUtils.CreateProfile("Default");

            LoadProfileSettings("Default");
        }

        private void PickColorButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ColorDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var wpfColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            ColorTextBox.Text = wpfColor.ToString();
        }

        private void PickOutlineColorButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ColorDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            var wpfColor = Color.FromArgb(dialog.Color.A, dialog.Color.R, dialog.Color.G, dialog.Color.B);
            OutlineColorTextBox.Text = wpfColor.ToString();
        }

        private void OnAdsSettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInternalUpdate) return;
            var isEnabled = EnableAdsProfileCheckBox.IsChecked == true;

            ConfigUtils.SetEnableAdsProfile(isEnabled);

            _mainWindow.SetAdsEnabled(isEnabled);
        }

        private void OnAdsProfileComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInternalUpdate || AdsProfileComboBox.SelectedItem == null) return;

            var selectedProfile = AdsProfileComboBox.SelectedItem.ToString();

            ConfigUtils.SetAdsProfile(selectedProfile);
            _mainWindow.SetAdsProfile(selectedProfile);
        }

        private void SaveChangesToProfile()
        {
            if (string.IsNullOrEmpty(_currentProfile)) return;
            if (StyleComboBox.SelectedItem == null) return;

            ConfigUtils.ConfigWrite(_currentProfile, "CrosshairStyle", StyleComboBox.SelectedItem.ToString());
            ConfigUtils.ConfigWrite(_currentProfile, "CrosshairColor", ColorTextBox.Text);
            ConfigUtils.ConfigWrite(_currentProfile, "OutlineColor", OutlineColorTextBox.Text);
            ConfigUtils.ConfigWrite(_currentProfile, "CrosshairLength", LengthSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "CrosshairLengthY", LengthYSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "LineThickness", ThicknessSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "LineThicknessY", ThicknessYSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "CrosshairGap", GapSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "RotationAngle", RotationSlider.Value.ToString("F0"));
            ConfigUtils.ConfigWrite(_currentProfile, "CrosshairOpacity", OpacitySlider.Value.ToString("F2"));
            ConfigUtils.ConfigWrite(_currentProfile, "OutlineThickness", OutlineThicknessSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "XOffset", XOffsetSlider.Value.ToString("F1"));
            ConfigUtils.ConfigWrite(_currentProfile, "YOffset", YOffsetSlider.Value.ToString("F1"));
        }

        private void UpdateCrosshair()
        {
            try
            {
                _mainWindow.UpdateCrosshairProperties(
                    (SolidColorBrush)new BrushConverter().ConvertFromString(ColorTextBox.Text),
                    (SolidColorBrush)new BrushConverter().ConvertFromString(OutlineColorTextBox.Text),
                    (CrosshairStyle)StyleComboBox.SelectedItem,
                    GapSlider.Value,
                    LengthSlider.Value,
                    LengthYSlider.Value,
                    OpacitySlider.Value,
                    ThicknessSlider.Value,
                    ThicknessYSlider.Value,
                    OutlineThicknessSlider.Value,
                    RotationSlider.Value,
                    XOffsetSlider.Value,
                    YOffsetSlider.Value);
            }
            catch (Exception)
            {
                // Ignore conversion errors while typing
            }
        }

        private void ToggleCrosshair_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Visibility =
                _mainWindow.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            ToggleCrosshairButton.Content =
                _mainWindow.Visibility == Visibility.Visible ? "Hide Crosshair" : "Show Crosshair";
        }
    }
}