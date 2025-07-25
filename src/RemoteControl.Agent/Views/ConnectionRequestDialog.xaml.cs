using RemoteControl.Agent.ViewModels;
using System.Windows;

namespace RemoteControl.Agent.Views
{
    /// <summary>
    /// Interaction logic for ConnectionRequestDialog.xaml
    /// </summary>
    public partial class ConnectionRequestDialog : Window
    {
        public ConnectionRequestDialog(AgentTrayViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Auto-close after 30 seconds if no action taken
            var autoCloseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            
            autoCloseTimer.Tick += (s, e) =>
            {
                autoCloseTimer.Stop();
                viewModel.RejectConnectionCommand.Execute(null);
                Close();
            };
            
            autoCloseTimer.Start();
            
            // Handle command execution to close dialog
            viewModel.AcceptConnectionCommand.CanExecuteChanged += (s, e) => Close();
            viewModel.RejectConnectionCommand.CanExecuteChanged += (s, e) => Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Flash the window to get attention
            FlashWindow();
            
            // Play system sound
            System.Media.SystemSounds.Exclamation.Play();
        }

        private void FlashWindow()
        {
            // Simple implementation - could use Win32 FlashWindowEx for more advanced flashing
            WindowState = WindowState.Minimized;
            WindowState = WindowState.Normal;
            Activate();
            Focus();
        }
    }
}
