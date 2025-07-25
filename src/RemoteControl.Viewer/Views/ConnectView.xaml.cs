using System.Windows.Controls;
using System.ComponentModel;
using RemoteControl.Viewer.ViewModels;

namespace RemoteControl.Viewer.Views
{
    /// <summary>
    /// Interaction logic for ConnectView.xaml
    /// </summary>
    public partial class ConnectView : UserControl
    {
        public ConnectView()
        {
            InitializeComponent();
            
            // Handle PasswordBox binding manually since WPF doesn't support it for security
            UserTokenBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is ConnectViewModel vm)
                {
                    vm.UserToken = UserTokenBox.Password;
                }
            };
            
            // Update PasswordBox when ViewModel property changes
            DataContextChanged += (s, e) =>
            {
                if (DataContext is ConnectViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(ConnectViewModel.UserToken))
                        {
                            if (UserTokenBox.Password != vm.UserToken)
                            {
                                UserTokenBox.Password = vm.UserToken;
                            }
                        }
                    };
                }
            };
        }
    }
}
