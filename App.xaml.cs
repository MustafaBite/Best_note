using System.Configuration;
using System.Data;
using System.Windows;

namespace NOT_VE_GÜNLÜK
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Verileri yükle 
            DataService.Load();

            // Giriş yapmış kullanıcı var mı?
            if (DataService.CurrentState.CurrentUserId != null)
            {
                HomePage home = new HomePage();
                home.Show();
            }
            else
            {
                LoginWindow login = new LoginWindow();
                login.Show();
            }
        }
    }

}
