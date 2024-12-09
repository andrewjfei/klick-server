using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace KlickServer.Hubs
{
    public class RoomHub: Hub
    {
        // HashSet to store rooms.
        private static readonly HashSet<string> Rooms = []; 

        // Dictionary to map user connection ids to user information.
        private static readonly ConcurrentDictionary<string, (string roomCode, string? name)> Users = new();

        public async Task<string> CreateRoom()
        {
            // Generate a unique room code.
            string roomCode = Guid.NewGuid().ToString()[..6].ToUpper();

            // Attempt to add the new room code to the rooms HashSet.
            if (Rooms.Add(roomCode))
            {
                Console.WriteLine($"Room {roomCode} created");

                // Associate user connection id with room.
                Users[Context.ConnectionId] = (roomCode, null);

                // Add user to room group.
                await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            }
            else 
            {
                Console.WriteLine($"Error: Room {roomCode} already exists");
            }

            return roomCode;
        }

        public async Task<bool> JoinRoom(string roomCode)
        {
            // Validate if room code exists.
            if (!Rooms.Contains(roomCode))
            {
                Console.WriteLine($"Room {roomCode} does not exist");
                return false;
            }

            // Associate user connection id with room.
            Users[Context.ConnectionId] = (roomCode, null);

            // Add user to room group.
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            Console.WriteLine($"{Context.ConnectionId} joined room {roomCode}");
            return true;
        }

        public async Task ChooseName(string name)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                // Update user room name.
                Users[Context.ConnectionId] = (user.roomCode, name);
                
                // Notify other users in the room that a new user has joined.
                await Clients.Group(user.roomCode)
                    .SendAsync("RecieveMessage", $"{name} has joined the room.");
            }
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var user))
            {
                if (user.name != null)
                {
                    // notify other users in the room that the user has left
                    await Clients.Group(user.roomCode)
                        .SendAsync("RecieveMessage", $"{user.name} has left the room.");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
