using RemoteControl.Viewer.ViewModels;
using System;
using System.Windows;

namespace RemoteControl.Viewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // Handle window closing
            Closing += (s, e) =>
            {
                // Allow ViewModel to handle cleanup
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ExitApplicationCommand.Execute(null);
                }
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize MainWindow: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }
}