using System.Collections.Concurrent;

namespace RemoteControl.ControlServer.Services;

/// <summary>
/// In-memory viewer service implementation
/// </summary>
public class ViewerService : IViewerService
{
    private readonly ILogger<ViewerService> _logger;
    private readonly ConcurrentDictionary<string, ViewerInfo> _viewers = new();

    public ViewerService(ILogger<ViewerService> logger)
    {
        _logger = logger;
    }

    public async Task RegisterViewerAsync(ViewerInfo viewerInfo)
    {
        _viewers[viewerInfo.ConnectionId] = viewerInfo;
        
        _logger.LogInformation("Viewer registered: {ConnectionId} for user {Username}",
            viewerInfo.ConnectionId, viewerInfo.Username);

        await Task.CompletedTask;
    }

    public async Task<ViewerInfo?> GetViewerByConnectionIdAsync(string connectionId)
    {
        _viewers.TryGetValue(connectionId, out var viewer);
        return await Task.FromResult(viewer);
    }

    public async Task<IEnumerable<ViewerInfo>> GetUserViewersAsync(string userId)
    {
        var userViewers = _viewers.Values
            .Where(v => v.UserId == userId && v.IsOnline)
            .ToList();

        return await Task.FromResult(userViewers);
    }

    public async Task SetViewerOfflineAsync(string connectionId)
    {
        if (_viewers.TryGetValue(connectionId, out var viewer))
        {
            viewer.IsOnline = false;
            viewer.LastSeen = DateTime.UtcNow;
            
            _logger.LogInformation("Viewer {ConnectionId} set offline", connectionId);
        }

        await Task.CompletedTask;
    }

    public async Task UpdateViewerHeartbeatAsync(string connectionId)
    {
        if (_viewers.TryGetValue(connectionId, out var viewer))
        {
            viewer.LastSeen = DateTime.UtcNow;
        }

        await Task.CompletedTask;
    }
} 