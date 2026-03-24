using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NOT_VE_GÜNLÜK
{
    public partial class JournalSettingsPage : Window, INotifyPropertyChanged
    {
        private JournalModel currentJournal;
        public DateTime? SelectedDate { get; private set; }

        private ObservableCollection<JournalEntry> _filteredEntries = new ObservableCollection<JournalEntry>();
        public ObservableCollection<JournalEntry> FilteredEntries
        {
            get => _filteredEntries;
            set { _filteredEntries = value; OnPropertyChanged(); }
        }

        public JournalSettingsPage(JournalModel journal)
        {
            InitializeComponent();
            currentJournal = journal;
            WindowHelper.Sync(this);
            DataContext = this;
            ApplyTheme();
            
            // Initial load: show only entries with titles as requested
            UpdateFilteredEntries();

            txtSearch.TextChanged += (s, e) => UpdateFilteredEntries();

            DataService.ThemeChanged += GlobalThemeChanged;
            DataService.BackgroundChanged += GlobalBackgroundChanged;
            this.Closed += (s, e) => 
            {
                DataService.ThemeChanged -= GlobalThemeChanged;
                DataService.BackgroundChanged -= GlobalBackgroundChanged;
            };
        }

        private void GlobalThemeChanged(string theme) => ApplyTheme();
        private void GlobalBackgroundChanged(string? path) => ApplyTheme();

        private void UpdateFilteredEntries()
        {
            if (currentJournal == null) return;

            string searchText = txtSearch.Text.ToLower().Trim();
            var entries = currentJournal.Entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Title)) // Only entries with titles
                .Where(e => {
                    if (string.IsNullOrEmpty(searchText)) return true;
                    
                    // Match title
                    if (e.Title.ToLower().Contains(searchText)) return true;
                    
                    // Match date (Turkish format)
                    string dateStr = e.Date.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR")).ToLower();
                    return dateStr.Contains(searchText);
                })
                .OrderByDescending(e => e.Date);

            FilteredEntries = new ObservableCollection<JournalEntry>(entries);
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            SelectedDate = DateTime.Now.Date;
            this.DialogResult = true;
            this.Close();
        }

        private void Entry_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is JournalEntry entry)
            {
                SelectedDate = entry.Date;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string theme)
            {
                DataService.NotifyThemeChanged(theme);
                DataService.Save();
            }
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyTheme(this);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
