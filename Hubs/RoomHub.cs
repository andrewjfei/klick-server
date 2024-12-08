using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace KlickServer.Hubs
{
    public class RoomHub: Hub
    {
        // dictionary to map user connection ids and their usernames
        private static readonly ConcurrentDictionary<string, (string roomId, string? name)> Users = new(); 

        public async Task JoinRoom(string roomId)
        {
            // associate user connection id with room
            Users[Context.ConnectionId] = (roomId, null);

            // add user to room
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        }

        public async Task SetName(string name)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                // update user session name
                Users[Context.ConnectionId] = (user.roomId, name);
                
                // notify other users in the room that a new user has joined
                await Clients.Group(user.roomId)
                    .SendAsync("JoinedRoom", $"{name} has joined the room.");
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var user))
            {
                if (user.name != null)
                {
                    // notify other users in the room that the user has left
                    await Clients.Group(user.roomId)
                        .SendAsync("LeftRoom", $"{user.name} has left the room.");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
