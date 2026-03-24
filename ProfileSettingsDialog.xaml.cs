using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace NOT_VE_GÜNLÜK
{
    public partial class ProfileSettingsDialog : Window
    {
        public string UserName { get; private set; } = "";
        public string? ProfilePhotoPath { get; private set; }
        public ImageSource? SelectedProfileImage { get; private set; }

        public ProfileSettingsDialog()
        {
            InitializeComponent();
            ApplyTheme();
            LoadCurrentData();
            DataService.ThemeChanged += GlobalThemeChanged;
            DataService.BackgroundChanged += GlobalBackgroundChanged;
            DataService.TextColorChanged += GlobalTextColorChanged;
            this.Closed += (s, e) => 
            {
                DataService.ThemeChanged -= GlobalThemeChanged;
                DataService.BackgroundChanged -= GlobalBackgroundChanged;
                DataService.TextColorChanged -= GlobalTextColorChanged;
            };
        }

        private void LoadCurrentData()
        {
            txtDisplayName.Text = DataService.CurrentState.UserName;
            ProfilePhotoPath = DataService.CurrentState.ProfilePhotoPath;

            if (!string.IsNullOrEmpty(ProfilePhotoPath) && System.IO.File.Exists(ProfilePhotoPath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(ProfilePhotoPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    var imageBrush = new ImageBrush(bitmap);
                    imageBrush.Stretch = Stretch.UniformToFill;
                    ProfileEllipse.Fill = imageBrush;
                    txtProfilePlaceholder.Visibility = Visibility.Collapsed;
                    SelectedProfileImage = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load existing profile image: {ex.Message}");
                }
            }
        }

        private void GlobalThemeChanged(string theme) => ApplyTheme();
        private void GlobalBackgroundChanged(string? path) => ApplyTheme();
        private void GlobalTextColorChanged(string? color) => ApplyTheme();

        private void ApplyTheme()
        {
            ThemeHelper.ApplyTheme(this);
            UpdateThemeButtons();
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            DataService.NotifyThemeChanged(tag);
            DataService.Save();
        }

        private void UpdateThemeButtons()
        {
            bool isDark = DataService.CurrentState.SelectedTheme == "Dark";
            
            // Highlight Light Theme Button
            btnLightTheme.BorderThickness = isDark ? new Thickness(1) : new Thickness(2.5);
            btnLightTheme.BorderBrush = isDark ? (Brush)FindResource("JournalBorder") : new SolidColorBrush(Color.FromRgb(59, 130, 246));
            btnLightTheme.Opacity = isDark ? 0.7 : 1.0;

            // Highlight Dark Theme Button
            btnDarkTheme.BorderThickness = isDark ? new Thickness(2.5) : new Thickness(1);
            btnDarkTheme.BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) : (Brush)FindResource("JournalBorder");
            btnDarkTheme.Opacity = isDark ? 1.0 : 0.7;
        }

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Resim Dosyaları (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tüm Dosyalar (*.*)|*.*";
            
            if (openFileDialog.ShowDialog() == true)
            {
                try 
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    var imageBrush = new ImageBrush(bitmap);
                    imageBrush.Stretch = Stretch.UniformToFill;
                    ProfileEllipse.Fill = imageBrush;
                    
                    
                    SelectedProfileImage = bitmap;
                    ProfilePhotoPath = openFileDialog.FileName;
                    txtProfilePlaceholder.Visibility = Visibility.Collapsed;

                    // Auto-save
                    DataService.CurrentState.ProfilePhotoPath = ProfilePhotoPath;
                    DataService.Save();
                }
                catch { }

            }
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (pbNewPassword.Visibility == Visibility.Visible)
            {
                txtNewPasswordVisible.Text = pbNewPassword.Password;
                pbNewPassword.Visibility = Visibility.Collapsed;
                txtNewPasswordVisible.Visibility = Visibility.Visible;
                tbEyeIcon.Text = "🙈";
            }
            else
            {
                pbNewPassword.Password = txtNewPasswordVisible.Text;
                txtNewPasswordVisible.Visibility = Visibility.Collapsed;
                pbNewPassword.Visibility = Visibility.Visible;
            }
        }

        private void txtDisplayName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataService.CurrentState != null)
            {
                DataService.CurrentState.UserName = txtDisplayName.Text;
                DataService.Save();
            }
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = pbNewPassword.Visibility == Visibility.Visible ? pbNewPassword.Password : txtNewPasswordVisible.Text;

            if (!string.IsNullOrEmpty(newPassword) && newPassword.Length != 8)
            {
                MessageBox.Show("Yeni şifre tam 8 karakter olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UserName = txtDisplayName.Text;
            
            if (!string.IsNullOrEmpty(newPassword) && DataService.CurrentState.CurrentUserId != null)
            {
                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DataService.DatabasePath}"))
                {
                    connection.Open();

                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "UPDATE Users SET Password = @p WHERE Id = @id";
                    cmd.Parameters.AddWithValue("@p", newPassword);
                    cmd.Parameters.AddWithValue("@id", DataService.CurrentState.CurrentUserId);
                    cmd.ExecuteNonQuery();
                }
            }

            DialogResult = true;
            Close();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Oturumu kapatmak istediğinize emin misiniz?", "Oturumu Kapat", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DataService.Logout();
                
                // Close all windows and show LoginWindow
                foreach (Window window in Application.Current.Windows)
                {
                    if (window != this) window.Close();
                }

                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string? color = btn.Tag?.ToString();
                if (color == "Auto") color = null;
                
                DataService.NotifyTextColorChanged(color);
                DataService.Save();

                // Update all windows immediately
                foreach (Window window in Application.Current.Windows)
                {
                    ThemeHelper.ApplyTheme(window);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
