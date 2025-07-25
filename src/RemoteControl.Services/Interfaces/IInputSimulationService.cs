using System;
using System.Threading;
using System.Threading.Tasks;
using RemoteControl.Core.Models;

namespace RemoteControl.Services.Interfaces
{
    public interface IInputSimulationService
    {
        Task SendMouseMoveAsync(int x, int y, CancellationToken cancellationToken = default);
        Task SendMouseClickAsync(int x, int y, int button, bool isPressed, CancellationToken cancellationToken = default);
        Task SendMouseWheelAsync(int x, int y, int delta, CancellationToken cancellationToken = default);
        Task SendKeyPressAsync(int keyCode, bool isPressed, CancellationToken cancellationToken = default);
        Task SendTextInputAsync(string text, CancellationToken cancellationToken = default);
        Task ProcessInputEventAsync(InputEvent inputEvent, CancellationToken cancellationToken = default);
        bool IsInputBlocked { get; set; }
    }
}
