using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NOT_VE_GÜNLÜK
{
    public partial class PlanPage : Window, INotifyPropertyChanged
    {
        private JournalModel currentJournal;
        
        private DateTime _currentDate = DateTime.Now;
        public DateTime CurrentDate 
        { 
            get => _currentDate;
            set
            {
                _currentDate = value;
                OnPropertyChanged();
            }
        }

        private bool IsHistoricalLocked => CurrentDate.Date < DateTime.Today;

        public PlanPage(JournalModel journal)
        {
            InitializeComponent();
            currentJournal = journal;
            this.Title = journal.Title;
            CurrentDate = DateTime.Now;
            WindowHelper.Sync(this);
            ApplyTheme();
            UpdateDate();
            LoadEntryForDate(CurrentDate);

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

        private void LoadEntryForDate(DateTime date)
        {
            var entry = currentJournal.Entries.FirstOrDefault(e => e.Date.Date == date.Date);
            if (entry != null)
            {
                txtDayTitle.Text = entry.Title;
                lstTasks.ItemsSource = new ObservableCollection<TaskItem>(entry.Tasks);
                
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
                txtDayTitle.Text = "";
                lstTasks.ItemsSource = new ObservableCollection<TaskItem>();
                pnlDayTitle.Visibility = Visibility.Collapsed;
                btnNameDay.Visibility = Visibility.Visible;
            }

            ApplyLocking();
        }

        private void ApplyLocking()
        {
            bool isLocked = IsHistoricalLocked;
            
            pnlNewTask.Visibility = isLocked ? Visibility.Collapsed : Visibility.Visible;
            btnNameDay.IsEnabled = !isLocked;
            btnEditTitle.Visibility = isLocked ? Visibility.Collapsed : Visibility.Visible;
            // The list itself handles interaction via Click handlers checking isLocked
        }

        private void SaveCurrentEntry()
        {
            if (currentJournal == null || IsHistoricalLocked) return;

            var entry = currentJournal.Entries.FirstOrDefault(e => e.Date.Date == CurrentDate.Date);
            
            // Collect tasks from ItemsSource
            var currentTasks = (lstTasks.ItemsSource as ObservableCollection<TaskItem>)?.ToList() ?? new System.Collections.Generic.List<TaskItem>();

            if (entry != null)
            {
                entry.Tasks = currentTasks;
            }
            else if (currentTasks.Count > 0 || !string.IsNullOrEmpty(txtDayTitle.Text))
            {
                entry = new JournalEntry
                {
                    Date = CurrentDate.Date,
                    Title = txtDayTitle.Text,
                    Tasks = currentTasks
                };
                currentJournal.Entries.Add(entry);
            }

            DataService.Save();
        }

        private void AddTask_Click(object? sender, RoutedEventArgs? e)
        {
            string taskTitle = txtTaskInput.Text.Trim();
            if (string.IsNullOrEmpty(taskTitle)) return;

            var tasks = lstTasks.ItemsSource as ObservableCollection<TaskItem>;
            if (tasks == null) 
            {
                tasks = new ObservableCollection<TaskItem>();
                lstTasks.ItemsSource = tasks;
            }

            tasks.Add(new TaskItem { Title = taskTitle, Status = TaskStatus.NotStarted });
            txtTaskInput.Clear();
            SaveCurrentEntry();
        }

        private void TaskInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTask_Click(null, null);
            }
        }

        private void Task_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsHistoricalLocked) return;
            if (sender is FrameworkElement el && el.DataContext is TaskItem task)
            {
                // Toggle between NotStarted/HalfDone and Completed
                task.Status = task.Status == TaskStatus.Completed ? TaskStatus.NotStarted : TaskStatus.Completed;
                RefreshTaskList();
                SaveCurrentEntry();
            }
        }

        private void Task_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsHistoricalLocked) return;
            if (sender is FrameworkElement el && el.DataContext is TaskItem task)
            {
                // Toggle HalfDone
                task.Status = task.Status == TaskStatus.HalfDone ? TaskStatus.NotStarted : TaskStatus.HalfDone;
                RefreshTaskList();
                SaveCurrentEntry();
            }
        }

        private void RefreshTaskList()
        {
            var tasks = lstTasks.ItemsSource;
            lstTasks.ItemsSource = null;
            lstTasks.ItemsSource = tasks;
        }

        private void TaskStatus_Changed(object sender, RoutedEventArgs e)
        {
            // Handled by mouse clicks now
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (IsHistoricalLocked) return;
            if (sender is Button btn && btn.DataContext is TaskItem task)
            {
                var tasks = lstTasks.ItemsSource as ObservableCollection<TaskItem>;
                tasks?.Remove(task);
                SaveCurrentEntry();
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
            SaveCurrentEntry();
            this.Close();
        }

        private void UpdateDate()
        {
            txtDate.Text = CurrentDate.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        }

        private void PrevDate_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            CurrentDate = CurrentDate.AddDays(-1);
            UpdateDate();
            LoadEntryForDate(CurrentDate);
        }

        private void NextDate_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            CurrentDate = CurrentDate.AddDays(1);
            UpdateDate();
            LoadEntryForDate(CurrentDate);
        }

        private void NameDay_Click(object sender, RoutedEventArgs e)
        {
            if (IsHistoricalLocked) return;
            var dialog = new InputDialog("Bu güne isim verin:");
            dialog.Owner = this;
            if (!string.IsNullOrEmpty(txtDayTitle.Text))
            {
                dialog.txtInput.Text = txtDayTitle.Text;
                dialog.txtInput.SelectAll();
            }

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                txtDayTitle.Text = dialog.InputText.Trim();
                pnlDayTitle.Visibility = Visibility.Visible;
                btnNameDay.Visibility = Visibility.Collapsed;
                SaveCurrentEntry();
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentEntry();
            var settings = new JournalSettingsPage(currentJournal);
            settings.Owner = this;
            if (settings.ShowDialog() == true && settings.SelectedDate.HasValue)
            {
                CurrentDate = settings.SelectedDate.Value;
                UpdateDate();
                LoadEntryForDate(CurrentDate);
            }
            ApplyTheme();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
