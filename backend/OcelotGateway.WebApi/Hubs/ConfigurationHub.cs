using Microsoft.AspNetCore.SignalR;

namespace OcelotGateway.WebApi.Hubs;

public class ConfigurationHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedGroup", groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftGroup", groupName);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    // Methods for broadcasting configuration changes
    public async Task NotifyRouteCreated(object routeData)
    {
        await Clients.All.SendAsync("RouteCreated", routeData);
    }

    public async Task NotifyRouteUpdated(object routeData)
    {
        await Clients.All.SendAsync("RouteUpdated", routeData);
    }

    public async Task NotifyRouteDeleted(Guid routeId)
    {
        await Clients.All.SendAsync("RouteDeleted", routeId);
    }

    public async Task NotifyVersionCreated(object versionData)
    {
        await Clients.All.SendAsync("VersionCreated", versionData);
    }

    public async Task NotifyVersionActivated(object versionData)
    {
        await Clients.All.SendAsync("VersionActivated", versionData);
    }

    public async Task NotifyConfigurationChanged(object configurationData)
    {
        await Clients.All.SendAsync("ConfigurationChanged", configurationData);
    }
} 