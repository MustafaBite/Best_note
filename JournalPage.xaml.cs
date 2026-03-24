using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace NOT_VE_GÜNLÜK
{
    public partial class JournalPage : Window
    {
        private DateTime currentDate = DateTime.Now.Date;
        private JournalModel currentJournal;

        public JournalPage(JournalModel journal)
        {
            InitializeComponent();
            currentJournal = journal;
            this.Title = journal.Title; // Set Window Title
            this.DataContext = currentJournal;
            WindowHelper.Sync(this);
            
            ApplyTheme();
            UpdateDate();
            LoadEntryForDate(currentDate);

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

        private void GlobalThemeChanged(string theme) => ApplyTheme();
        private void GlobalBackgroundChanged(string? path) => ApplyTheme();
        private void GlobalTextColorChanged(string? color) => ApplyTheme();
        
        // Default constructor for designer/fallback, though checking null journal is wise
        public JournalPage() : this(new JournalModel { Title = "Demo" }) { }

        private void LoadEntryForDate(DateTime date)
        {
            if (currentJournal == null) return;

            var entry = currentJournal.Entries.FirstOrDefault(e => e.Date.Date == date.Date);
            if (entry != null)
            {
                txtDayTitle.Text = entry.Title;
                txtContent.Text = entry.Content;
                
                if (!string.IsNullOrEmpty(entry.Title))
                {
                    pnlDayTitle.Visibility = Visibility.Visible;
                    btnNameDay.Visibility = Visibility.Collapsed;
                }
                else
                {
                    pnlDayTitle.Visibility = Visibility.Collapsed;
                    btnNameDay.Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Clear fields for new day
                txtDayTitle.Text = "";
                txtContent.Text = "";
                pnlDayTitle.Visibility = Visibility.Collapsed;
                btnNameDay.Visibility = Visibility.Visible;
            }
        }

        private void SaveCurrentEntry()
        {
            if (currentJournal == null) return;
            
            // Don't save if empty AND no title (unless it was previously saved, in which case maybe we should?)
            // If Text is empty and Title is empty, we can choose to delete entry or just save empty.
            // Let's save if there is content OR title.
            
            string content = txtContent.Text;
            string title = txtDayTitle.Text;
            
            // Check if entry exists
            var entry = currentJournal.Entries.FirstOrDefault(e => e.Date.Date == currentDate.Date);
            
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(title) && !btnNameDay.IsVisible)
            {
                // Empty, remove if exists? Or just ignore.
                // If the user manually cleared it, we might want to keep the entry but empty?
                // For simplicity, let's upsert.
            }
            
            if (entry != null)
            {
                entry.Content = content;
                entry.Title = title;
            }
            else
            {
                // Only create new entry if there is something to save
                if (!string.IsNullOrWhiteSpace(content) || (!string.IsNullOrWhiteSpace(title)))
                {
                    entry = new JournalEntry
                    {
                        Date = currentDate.Date,
                        Title = title,
                        Content = content
                    };
                    currentJournal.Entries.Add(entry);
                }
            }

            // Sync to disk immediately to satisfy "even if app crashes" robustness (optional but safer)
            DataService.Save(); 
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            this.Close();
        }
        
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveCurrentEntry();
            base.OnClosing(e);
        }

        private void UpdateDate()
        {
            txtDate.Text = currentDate.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string theme)
            {
                currentJournal.SelectedTheme = theme;
                DataService.NotifyThemeChanged(theme);
                DataService.Save();
            }
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyTheme(this);
        }

        private void NameDay_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Bu güne isim verin:");
            dialog.Owner = this;
            
            // Pre-fill if already named
            if (!string.IsNullOrEmpty(txtDayTitle.Text))
            {
                dialog.txtInput.Text = txtDayTitle.Text;
                dialog.txtInput.SelectAll();
            }
            
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                string newTitle = dialog.InputText.Trim();
                
                // Check for duplicate titles in this journal
                bool duplicateExists = currentJournal.Entries.Any(e => 
                    e.Date.Date != currentDate.Date && 
                    string.Equals(e.Title, newTitle, StringComparison.OrdinalIgnoreCase));

                if (duplicateExists)
                {
                    MessageBox.Show("Bu başlığa sahip başka bir gün bulunuyor. Lütfen farklı bir isim veriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                txtDayTitle.Text = newTitle;
                pnlDayTitle.Visibility = Visibility.Visible;
                btnNameDay.Visibility = Visibility.Collapsed;
                SaveCurrentEntry(); // Save immediately after naming
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            var settingsPage = new JournalSettingsPage(currentJournal);
            settingsPage.Owner = this;
            
            if (settingsPage.ShowDialog() == true && settingsPage.SelectedDate.HasValue)
            {
                // Navigate to the selected date
                currentDate = settingsPage.SelectedDate.Value;
                UpdateDate();
                LoadEntryForDate(currentDate);
            }
            
            ApplyTheme(); // Re-apply in case it changed in settings
        }

        private void PreviousDay_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            currentDate = currentDate.AddDays(-1);
            UpdateDate();
            LoadEntryForDate(currentDate);
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            currentDate = currentDate.AddDays(1);
            UpdateDate();
            LoadEntryForDate(currentDate);
        }

        private void txtContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            SaveCurrentEntry();
        }
    }
}

