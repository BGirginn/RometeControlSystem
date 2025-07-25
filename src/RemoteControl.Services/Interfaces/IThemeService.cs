using System;
using System.Threading.Tasks;

namespace RemoteControl.Services.Interfaces
{
    public interface IThemeService
    {
        Task<string> GetThemeAsync();
        Task SetThemeAsync(string theme);
        event EventHandler<string>? ThemeChanged;
    }
}
