using System.Windows;
using System.Windows.Input;

namespace NOT_VE_GÜNLÜK
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
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

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text?.Trim() ?? "";
            string password = pbPassword.Visibility == Visibility.Visible ? pbPassword.Password : txtPasswordVisible.Text;

            if (string.IsNullOrEmpty(username) || password.Length != 8)
            {
                MessageBox.Show("Lütfen geçerli bir kullanıcı adı ve 8 karakterli şifre girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataService.Login(username, password))
            {
                HomePage home = new HomePage();
                home.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Kayıtlı kullanıcı bulunamadı veya şifre yanlış.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
