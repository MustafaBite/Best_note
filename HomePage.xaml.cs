using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NOT_VE_GÜNLÜK
{
    public partial class HomePage : Window, INotifyPropertyChanged
    {
        public ObservableCollection<JournalModel> Journals { get; set; }

        private string _userName = "Admin";
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private ImageSource? _profileImageSource;
        public ImageSource? ProfileImageSource
        {
            get => _profileImageSource;
            set
            {
                _profileImageSource = value;
                OnPropertyChanged();
            }
        }

        public HomePage()
        {
            InitializeComponent();
            DataContext = this;
            WindowHelper.Sync(this);

            // Load Data
            DataService.Load();
            
            // Initialize UI from Data
            UserName = DataService.CurrentState.UserName;
            LoadProfileImage(DataService.CurrentState.ProfilePhotoPath ?? "");
            Journals = new ObservableCollection<JournalModel>(DataService.CurrentState.Journals);
            
            // Apply global theme
            ApplyTheme();

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

        private void LoadProfileImage(string? path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                // Default placeholder
                // Use a default colored brush or image if needed. For now, creating a simple default bitmap or just leaving it null might show blank. 
                // Let's create a dynamic default if null.
                // Actually, passing a default "A" image or similar would be better, but for now we'll handle the null case in UI or use a fallback.
                ProfileImageSource = null; 
                return; 
            }

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ProfileImageSource = bitmap;
            }
            catch
            {
                // Fallback
                ProfileImageSource = null;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var profileSettings = new ProfileSettingsDialog();
            profileSettings.Owner = this;
            
            if (profileSettings.ShowDialog() == true)
            {
                // Refresh UI from CurrentState (Dialog handles its own saving now)
                UserName = DataService.CurrentState.UserName;
                LoadProfileImage(DataService.CurrentState.ProfilePhotoPath);
            }

        }

        private void AddJournal_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            
            var itemJournal = new MenuItem { Header = "📖 Günlük Ekle" };
            itemJournal.Click += (s, args) => CreateJournalOfType(JournalType.Journal);
            
            var itemPlan = new MenuItem { Header = "📅 Not / Plan Ekle" };
            itemPlan.Click += (s, args) => CreateJournalOfType(JournalType.Plan);

            var itemBackground = new MenuItem { Header = "🖼️ Arka Plan Değiştir" };
            itemBackground.Click += ChangeBackground_Click;

            var itemResetBg = new MenuItem { Header = "🔄 Varsayılan Arka Plana Dön" };
            itemResetBg.Click += ResetBackground_Click;
            
            menu.Items.Add(itemJournal);
            menu.Items.Add(itemPlan);
            menu.Items.Add(new Separator());
            menu.Items.Add(itemBackground);
            menu.Items.Add(itemResetBg);
            
            menu.PlacementTarget = sender as Button;
            menu.IsOpen = true;
        }

        private void CreateJournalOfType(JournalType type)
        {
            var createDialog = new CreateJournalDialog();
            createDialog.Owner = this;
            if (createDialog.ShowDialog() == true)
            {
                var newJournal = new JournalModel 
                { 
                    Title = type == JournalType.Journal ? $"📔 {createDialog.JournalName}" : $"📝 {createDialog.JournalName}", 
                    UpdateTimeDescription = "Yeni oluşturuldu", 
                    Icon = "→",
                    Type = type
                };

                Journals.Add(newJournal);
                
                DataService.CurrentState.Journals = Journals.ToList();
                DataService.Save();
            }
        }

        private void ChangeBackground_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Resim Dosyaları (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Tüm Dosyalar (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                DataService.NotifyBackgroundChanged(openFileDialog.FileName);
                DataService.Save();
            }
        }

        private void ResetBackground_Click(object sender, RoutedEventArgs e)
        {
            DataService.NotifyBackgroundChanged(null);
            DataService.Save();
        }

        private void DeleteJournal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JournalModel journal)
            {
                var result = MessageBox.Show($"'{journal.Title}' günlüğünü silmek istediğinize emin misiniz?", "Günlük Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Journals.Remove(journal);
                    
                    // Save to Persistence
                    DataService.CurrentState.Journals = Journals.ToList();
                    DataService.Save();
                }
            }
            // Prevent the Border's MouseLeftButtonDown from firing which opens the journal
            e.Handled = true; 
        }

        private void JournalCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is JournalModel journal)
            {
                Window targetPage;
                if (journal.Type == JournalType.Plan)
                    targetPage = new PlanPage(journal);
                else
                    targetPage = new JournalPage(journal);

                targetPage.Owner = this;
                
                this.Hide(); 
                targetPage.Show();
                
                targetPage.Closed += (s, args) => 
                {
                    this.Show();
                    DataService.Save();
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

