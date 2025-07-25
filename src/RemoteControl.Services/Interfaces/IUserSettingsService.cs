using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteControl.Core.Models;

namespace RemoteControl.Services.Interfaces
{
    public interface IUserSettingsService
    {
        Task<string> GetUserTokenAsync();
        Task SaveUserTokenAsync(string token);
        Task AddRecentConnectionAsync(string targetId);
        Task<IEnumerable<RecentConnection>> GetRecentConnectionsAsync();
        Task<string> GetThemeAsync();
        Task SetThemeAsync(string theme);
    }
}