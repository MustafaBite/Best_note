using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NOT_VE_GÜNLÜK
{
    public static class ThemeHelper
    {
        public static void ApplyTheme(Window window)
        {
            if (window == null) return;

            try
            {
                // 1. Get current theme and background path from DataService
                var state = DataService.CurrentState;
                string themeKey = (state.SelectedTheme == "Light") ? "LightTheme" : "DarkTheme";
                
                // 2. Find and apply the base theme dictionary
                var themeDict = Application.Current.FindResource(themeKey) as ResourceDictionary;
                if (themeDict != null)
                {
                    window.Resources.MergedDictionaries.Clear();
                    window.Resources.MergedDictionaries.Add(themeDict);
                }

                // 3. Determine if the base theme is "Dark"
                bool isBackgroundDark = (state.SelectedTheme == "Dark");

                // 4. Handle Custom Background
                if (!string.IsNullOrEmpty(state.CustomBackgroundPath) && File.Exists(state.CustomBackgroundPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(state.CustomBackgroundPath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        var imageBrush = new ImageBrush(bitmap);
                        imageBrush.Stretch = Stretch.UniformToFill;
                        window.Resources["JournalBackground"] = imageBrush;

                        // We keep the manually selected theme (isBackgroundDark) as the driver 
                        // for UI colors, but we could still use brightness detection for "Auto" logic if needed.
                        // For now, respect the manual "Aydınlık/Karanlık" selection.
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ThemeHelper: Custom background failed: {ex.Message}");
                    }
                }
                else
                {
                    // If no custom background, remove any override from resources
                    // This allows the theme dict's "JournalBackground" to be used (if any)
                    if (window.Resources.Contains("JournalBackground"))
                    {
                        window.Resources.Remove("JournalBackground");
                    }
                }

                // 5. Apply adaptive text and container colors to window resources
                if (isBackgroundDark)
                {
                    // Default for Dark backgrounds: White text
                    window.Resources["JournalForeground"] = new SolidColorBrush(Colors.White);
                    window.Resources["JournalSecondaryForeground"] = new SolidColorBrush(Color.FromRgb(226, 232, 240)); 
                    
                    // Containers should be dark to contrast with white text
                    window.Resources["JournalButtonBackground"] = new SolidColorBrush(Color.FromArgb(180, 15, 23, 42)); // Dark Slate with transparency
                    window.Resources["JournalButtonHover"] = new SolidColorBrush(Color.FromArgb(220, 30, 41, 59));
                    window.Resources["JournalCardBackground"] = new SolidColorBrush(Color.FromArgb(180, 30, 41, 59));
                    
                    // Also ensure the overlay is dark if we decided background is dark
                    window.Resources["JournalOverlay"] = new SolidColorBrush(Color.FromArgb(100, 15, 23, 42));
                }
                else
                {
                    // Default for Light backgrounds: Very dark slate text for maximum readability
                    window.Resources["JournalForeground"] = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                    window.Resources["JournalSecondaryForeground"] = new SolidColorBrush(Color.FromRgb(71, 85, 105)); 
                    
                    // Containers should be white/light but more solid than before
                    window.Resources["JournalButtonBackground"] = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)); // More opaque white
                    window.Resources["JournalButtonHover"] = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                    window.Resources["JournalCardBackground"] = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255));

                    // Use a soft white overlay to reduce the image's "busyness" but keep the aesthetic
                    window.Resources["JournalOverlay"] = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
                }

                // 6. Override Foreground if custom color is selected
                if (!string.IsNullOrEmpty(state.SelectedTextColor))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(state.SelectedTextColor);
                        window.Resources["JournalForeground"] = new SolidColorBrush(color);
                        
                        // Also adjust secondary foreground slightly translucent
                        var secondaryColor = color;
                        secondaryColor.A = 180;
                        window.Resources["JournalSecondaryForeground"] = new SolidColorBrush(secondaryColor);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeHelper: ApplyTheme failed for {window.GetType().Name}: {ex.Message}");
            }
        }
    }
}
