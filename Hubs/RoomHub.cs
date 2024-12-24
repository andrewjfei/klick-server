using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using KlickServer.Models;

namespace KlickServer.Hubs
{
    public class RoomHub : Hub
    {
        // HashSet to store rooms.
        private static readonly ConcurrentDictionary<string, (List<string>, List<Team>)> Rooms = [];

        // Dictionary to map user connection ids to user information.
        private static readonly ConcurrentDictionary<
            string,
            (string roomCode, string? name)
        > Users = new();

        public async Task<string> CreateRoom()
        {
            // Generate a unique room code.
            string roomCode = Guid.NewGuid().ToString()[..6].ToUpper();

            // Attempt to add the new room code to the rooms HashSet.
            if (Rooms.TryAdd(roomCode, ([], [])))
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

        public async Task<string?> AddCriterion(string criterion)
        {
            return await Task.Run(() =>
            {
                if (Users.TryGetValue(Context.ConnectionId, out var user))
                {
                    if (Rooms.TryGetValue(user.roomCode, out var roomData))
                    {
                        List<string> criteria = roomData.Item1;

                        criteria.Add(criterion);

                        Console.WriteLine($"Added criterion {criterion} to room {user.roomCode}.");

                        return criterion;
                    }
                }

                return null;
            });
        }

        public async Task<string?> AddTeam(string teamName)
        {
            return await Task.Run(() =>
            {
                if (Users.TryGetValue(Context.ConnectionId, out var user))
                {
                    if (Rooms.TryGetValue(user.roomCode, out var roomData))
                    {
                        List<Team> teams = roomData.Item2;

                        Team team = new(teamName);

                        teams.Add(team);

                        Console.WriteLine($"Added team {teamName} to room {user.roomCode}");

                        return team.Id.ToString();
                    }
                }

                return null;
            });
        }

        public async Task StartScoring(string teamId) 
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                if (Rooms.TryGetValue(user.roomCode, out var roomData))
                {
                    List<string> criteria = roomData.Item1;
                    List<Team> teams = roomData.Item2;

                    if (teams.Any((team) => team.Id.ToString() == teamId))
                    {
                        Team? team = teams.Find((team) => team.Id.ToString() == teamId);

                        if (team != null) {
                            Console.WriteLine($"Start scoring for team {team.Name}");

                            // Notify users in the room to start scoring.
                            await Clients.Group(user.roomCode).SendAsync("StartScoring", criteria, team.Id, team.Name);
                        } else {
                            Console.WriteLine($"Team with id {teamId} does not exist.");
                        }
                    }
                }
            }
        }

        public async Task<bool> JoinRoom(string roomCode)
        {
            // Validate if room code exists.
            if (!Rooms.ContainsKey(roomCode))
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
                await Clients.Group(user.roomCode).SendAsync("JoinedRoom", Context.ConnectionId, name);
            }
        }

        public async Task GiveScore(string teamId, int score)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                if (Rooms.TryGetValue(user.roomCode, out var roomData))
                {

                    List<Team> teams = roomData.Item2;
                    
                    if (teams.Any((team) => team.Id.ToString() == teamId))
                    {
                        Team? team = teams.Find((team) => team.Id.ToString() == teamId);

                        if (team != null) {
                            team.AddScore(score);

                            Console.WriteLine($"{user.name} gave a score of {score} for team {team.Name}.");

                            // Notify host that the team score has been updated.
                            await Clients.Group(user.roomCode).SendAsync("UpdateScore", Context.ConnectionId, team.Id, team.Score);
                        } else {
                            Console.WriteLine($"Team with id {teamId} does not exist.");
                        }
                    }
                }
            }
        }
        
        public async Task SendMessage(string message)
        {
            if (Users.TryGetValue(Context.ConnectionId, out var user))
            {
                // Notify host that there is a user has sent a message.
                await Clients.Group(user.roomCode).SendAsync("Message", Context.ConnectionId, message);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Users.TryRemove(Context.ConnectionId, out var user))
            {
                if (user.name != null)
                {
                    // Notify other users in the room that the user has left.
                    await Clients.Group(user.roomCode).SendAsync("LeftRoom", Context.ConnectionId, user.name);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
