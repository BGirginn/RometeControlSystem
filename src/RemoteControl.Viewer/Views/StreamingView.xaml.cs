using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace RemoteControl.Viewer.Views
{
    /// <summary>
    /// Interaction logic for StreamingView.xaml
    /// </summary>
    public partial class StreamingView : UserControl
    {
        public StreamingView()
        {
            InitializeComponent();
            
            // Ensure the control can receive focus for keyboard input
            Focusable = true;
            Focus();
        }

        private void StreamingView_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle keyboard input for remote control
            if (DataContext is ViewModels.StreamingViewModel viewModel && viewModel.InputEnabled)
            {
                // TODO: Send keyboard input to remote computer via transport service
                // This would be implemented with the actual transport service
                
                // Handle special keys
                switch (e.Key)
                {
                    case Key.F11:
                        viewModel.ToggleFullscreenCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        if (viewModel.IsFullscreen)
                        {
                            viewModel.ToggleFullscreenCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;
                }
            }
        }

        private void StreamingView_MouseMove(object sender, MouseEventArgs e)
        {
            // Handle mouse movement for remote control
            if (DataContext is ViewModels.StreamingViewModel viewModel && viewModel.InputEnabled)
            {
                // TODO: Send mouse movement to remote computer via transport service
                var position = e.GetPosition(RemoteScreenImage);
                
                // Calculate relative position (0-1) for transmission
                var relativeX = position.X / RemoteScreenImage.ActualWidth;
                var relativeY = position.Y / RemoteScreenImage.ActualHeight;
                
                // Ensure coordinates are within bounds
                relativeX = Math.Max(0, Math.Min(1, relativeX));
                relativeY = Math.Max(0, Math.Min(1, relativeY));
                
                // TODO: Send via transport service
                // await _transportService.SendMouseMoveAsync(relativeX, relativeY);
            }
        }

        private void StreamingView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Handle mouse clicks for remote control
            if (DataContext is ViewModels.StreamingViewModel viewModel && viewModel.InputEnabled)
            {
                // TODO: Send mouse button down to remote computer
                var button = e.ChangedButton switch
                {
                    MouseButton.Left => "Left",
                    MouseButton.Right => "Right",
                    MouseButton.Middle => "Middle",
                    _ => "Unknown"
                };
                
                // TODO: Send via transport service
                // await _transportService.SendMouseDownAsync(button);
                
                // Handle double-click for fullscreen toggle
                if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
                {
                    viewModel.ToggleFullscreenCommand.Execute(null);
                }
            }
            
            // Ensure focus for keyboard input
            Focus();
        }

        private void StreamingView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Handle mouse button release for remote control
            if (DataContext is ViewModels.StreamingViewModel viewModel && viewModel.InputEnabled)
            {
                // TODO: Send mouse button up to remote computer
                var button = e.ChangedButton switch
                {
                    MouseButton.Left => "Left",
                    MouseButton.Right => "Right",
                    MouseButton.Middle => "Middle",
                    _ => "Unknown"
                };
                
                // TODO: Send via transport service
                // await _transportService.SendMouseUpAsync(button);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            
            // Handle mouse wheel for remote control
            if (DataContext is ViewModels.StreamingViewModel viewModel && viewModel.InputEnabled)
            {
                // TODO: Send mouse wheel delta to remote computer
                var delta = e.Delta;
                
                // TODO: Send via transport service
                // await _transportService.SendMouseWheelAsync(delta);
            }
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Ensure focus when the control loads
            Focus();
        }

        private void UserControl_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            // Visual indication that the control has focus for input
            // Could add a subtle border or other visual cue here
        }

        private void UserControl_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            // Handle loss of focus if needed
        }
    }
}
