using System.Windows;
using System.Windows.Input;

namespace NOT_VE_GÜNLÜK
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            WindowHelper.Sync(this);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (pbPassword.Visibility == Visibility.Visible)
            {
                txtPasswordVisible.Text = pbPassword.Password;
                pbPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                tbEyeIcon.Text = "🙈";
            }
            else
            {
                pbPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                pbPassword.Visibility = Visibility.Visible;
                tbEyeIcon.Text = "👁️";
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string displayName = txtDisplayName.Text;
            string password = pbPassword.Visibility == Visibility.Visible ? pbPassword.Password : txtPasswordVisible.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(displayName) || password.Length != 8)
            {
                MessageBox.Show("Lütfen tüm alanları doldurun ve şifrenin 8 karakter olduğundan emin olun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataService.Register(username, password, displayName))
            {
                MessageBox.Show("Kayıt başarılı! Şimdi giriş yapabilirsiniz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Kayıt başarısız. Bu kullanıcı adı zaten kullanılıyor olabilir.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoginWindow_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
