using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NOT_VE_GÜNLÜK
{
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; } = "";

        public InputDialog(string prompt = "Lütfen girin:")
        {
            InitializeComponent();
            txtPrompt.Text = prompt;
            txtInput.Focus();
            ApplyTheme();
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

        private void Theme_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            DataService.NotifyThemeChanged(tag);
            DataService.Save();
        }

        private void ApplyTheme()
        {
            ThemeHelper.ApplyTheme(this);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            InputText = txtInput.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
