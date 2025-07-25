using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RemoteControl.Core.Models;
using RemoteControl.Services.Interfaces;

namespace RemoteControl.Services.Implementations
{
    public class WindowsInputSimulationService : IInputSimulationService
    {
        private readonly ILogger<WindowsInputSimulationService> _logger;

        public bool IsInputBlocked { get; set; }

        public WindowsInputSimulationService(ILogger<WindowsInputSimulationService> logger)
        {
            _logger = logger;
        }

        public async Task SendMouseMoveAsync(int x, int y, CancellationToken cancellationToken = default)
        {
            if (IsInputBlocked) return;

            await Task.Run(() =>
            {
                SetCursorPos(x, y);
            }, cancellationToken);
        }

        public async Task SendMouseClickAsync(int x, int y, int button, bool isPressed, CancellationToken cancellationToken = default)
        {
            if (IsInputBlocked) return;

            await Task.Run(() =>
            {
                SetCursorPos(x, y);
                
                uint mouseEvent = button switch
                {
                    0 => isPressed ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP,   // Left button
                    1 => isPressed ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP, // Right button
                    2 => isPressed ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP, // Middle button
                    _ => 0
                };

                if (mouseEvent != 0)
                {
                    mouse_event(mouseEvent, (uint)x, (uint)y, 0, UIntPtr.Zero);
                }
            }, cancellationToken);
        }

        public async Task SendMouseWheelAsync(int x, int y, int delta, CancellationToken cancellationToken = default)
        {
            if (IsInputBlocked) return;

            await Task.Run(() =>
            {
                SetCursorPos(x, y);
                mouse_event(MOUSEEVENTF_WHEEL, (uint)x, (uint)y, (uint)delta, UIntPtr.Zero);
            }, cancellationToken);
        }

        public async Task SendKeyPressAsync(int keyCode, bool isPressed, CancellationToken cancellationToken = default)
        {
            if (IsInputBlocked) return;

            await Task.Run(() =>
            {
                uint keyEvent = isPressed ? 0 : KEYEVENTF_KEYUP;
                keybd_event((byte)keyCode, 0, keyEvent, UIntPtr.Zero);
            }, cancellationToken);
        }

        public async Task SendTextInputAsync(string text, CancellationToken cancellationToken = default)
        {
            if (IsInputBlocked) return;

            await Task.Run(() =>
            {
                foreach (char c in text)
                {
                    // Use Unicode input for text
                    keybd_event(0, 0, KEYEVENTF_UNICODE, (UIntPtr)c);
                    keybd_event(0, 0, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP, (UIntPtr)c);
                }
            }, cancellationToken);
        }

        public async Task ProcessInputEventAsync(InputEvent inputEvent, CancellationToken cancellationToken = default)
        {
            if (IsInputBlocked) return;

            try
            {
                switch (inputEvent.Type)
                {
                    case InputEventType.MouseMove:
                        await SendMouseMoveAsync(inputEvent.X, inputEvent.Y, cancellationToken);
                        break;
                    
                    case InputEventType.MouseClick:
                        await SendMouseClickAsync(inputEvent.X, inputEvent.Y, inputEvent.Button, inputEvent.IsPressed, cancellationToken);
                        break;
                    
                    case InputEventType.MouseWheel:
                        await SendMouseWheelAsync(inputEvent.X, inputEvent.Y, inputEvent.Button, cancellationToken);
                        break;
                    
                    case InputEventType.KeyPress:
                    case InputEventType.KeyRelease:
                        await SendKeyPressAsync(inputEvent.Button, inputEvent.Type == InputEventType.KeyPress, cancellationToken);
                        break;
                    
                    case InputEventType.TextInput:
                        if (!string.IsNullOrEmpty(inputEvent.Text))
                        {
                            await SendTextInputAsync(inputEvent.Text, cancellationToken);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing input event: {InputEventType}", inputEvent.Type);
                throw;
            }
        }

        // Windows API functions
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // Mouse event constants
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        // Keyboard event constants
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
    }
}
