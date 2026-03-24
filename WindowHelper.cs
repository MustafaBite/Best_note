using System.Windows;

namespace NOT_VE_GÜNLÜK
{
    public static class WindowHelper
    {
        public static void Sync(Window window)
        {
            if (window == null) return;

            // Apply initial state
            if (DataService.CurrentState.IsMaximized)
            {
                window.WindowState = WindowState.Maximized;
            }

            // Sync state changes
            window.StateChanged += (s, e) =>
            {
                if (window.WindowState == WindowState.Maximized)
                {
                    DataService.CurrentState.IsMaximized = true;
                }
                else if (window.WindowState == WindowState.Normal)
                {
                    DataService.CurrentState.IsMaximized = false;
                }
                
                // Save state immediately to persistence
                DataService.Save();
            };
        }
    }
}
