using HeadFootball.Shared;
using Newtonsoft.Json;

namespace HeadFootball.Server
{
    public class LobbyRoom
    {
        public string RoomId { get; } = Guid.NewGuid().ToString()[..8];
        public ClientHandler? Player1 { get; set; }
        public ClientHandler? Player2 { get; set; }
        public List<ClientHandler> Spectators { get; } = new();
        public GameRoom? Game { get; set; }
        public bool InProgress => Game != null;

        public int PlayerCount => (Player1 != null ? 1 : 0) + (Player2 != null ? 1 : 0);
    }

    public class LobbyManager
    {
        private readonly List<ClientHandler> _clients = new();
        private readonly List<LobbyRoom> _rooms = new();
        private readonly Database _db;
        private readonly object _lock = new();

        public LobbyManager(Database db)
        {
            _db = db;
        }

        public void AddClient(ClientHandler client)
        {
            lock (_lock)
            {
                _clients.Add(client);
                Console.WriteLine($"Client conectat. Total: {_clients.Count}");
            }
        }

        public void RemoveClient(ClientHandler client)
        {
            lock (_lock)
            {
                _clients.Remove(client);

                // Scoatem clientul din orice camera
                foreach (var room in _rooms.ToList())
                {
                    if (room.Player1 == client) room.Player1 = null;
                    if (room.Player2 == client) room.Player2 = null;
                    room.Spectators.Remove(client);

                    // Stergem camera daca e goala
                    if (room.Player1 == null && room.Player2 == null
                        && room.Spectators.Count == 0)
                    {
                        _rooms.Remove(room);
                        Console.WriteLine($"Camera {room.RoomId} stearsa.");
                    }
                }

                BroadcastRoomList();
            }
        }

        public void CreateRoom(ClientHandler creator)
        {
            lock (_lock)
            {
                var room = new LobbyRoom();
                room.Player1 = creator;
                _rooms.Add(room);

                Console.WriteLine($"[{creator.Username}] a creat camera {room.RoomId}");

                // Confirmam creatorului
                creator.Send(new NetworkMessage
                {
                    Type = MessageType.RoomJoined,
                    Payload = JsonConvert.SerializeObject(new JoinRoomPayload
                    {
                        RoomId = room.RoomId,
                        AsSpectator = false
                    })
                });

                BroadcastRoomList();
            }
        }

        public void JoinRoom(ClientHandler client, string roomId, bool asSpectator)
        {
            lock (_lock)
            {
                var room = _rooms.FirstOrDefault(r => r.RoomId == roomId);
                if (room == null)
                {
                    client.Send(new NetworkMessage
                    {
                        Type = MessageType.LoginFail,
                        Payload = JsonConvert.SerializeObject(new LoginResultPayload
                        {
                            Success = false,
                            Message = "Camera nu exista."
                        })
                    });
                    return;
                }

                if (asSpectator)
                {
                    room.Spectators.Add(client);
                    Console.WriteLine($"[{client.Username}] urmareste camera {roomId}");
                }
                else
                {
                    if (room.Player2 != null)
                    {
                        client.Send(new NetworkMessage
                        {
                            Type = MessageType.LoginFail,
                            Payload = JsonConvert.SerializeObject(new LoginResultPayload
                            {
                                Success = false,
                                Message = "Camera este plina."
                            })
                        });
                        return;
                    }

                    room.Player2 = client;
                    Console.WriteLine($"[{client.Username}] a intrat in camera {roomId}");
                }

                client.Send(new NetworkMessage
                {
                    Type = MessageType.RoomJoined,
                    Payload = JsonConvert.SerializeObject(new JoinRoomPayload
                    {
                        RoomId = roomId,
                        AsSpectator = asSpectator
                    })
                });

                BroadcastRoomList();

                // Daca sunt 2 jucatori, pornim jocul
                if (room.Player1 != null && room.Player2 != null && !room.InProgress)
                    StartGame(room);
            }
        }

        private void StartGame(LobbyRoom room)
        {
            Console.WriteLine($"Pornesc jocul in camera {room.RoomId}!");

            var game = new GameRoom(
                room.Player1!,
                room.Player2!,
                room.Spectators,
                _db
            );

            room.Game = game;
            game.Start();
        }

        public void ForwardInput(ClientHandler client, NetworkMessage msg)
        {
            lock (_lock)
            {
                var room = _rooms.FirstOrDefault(r =>
                    r.Player1 == client || r.Player2 == client);
                room?.Game?.ReceiveInput(client, msg);
            }
        }

        public void BroadcastChat(ChatPayload chat)
        {
            var msg = new NetworkMessage
            {
                Type = MessageType.ChatMessage,
                Payload = JsonConvert.SerializeObject(chat)
            };

            lock (_lock)
            {
                // Chat in lobby — trimitem la toti
                if (chat.Room == "lobby")
                    foreach (var c in _clients)
                        c.Send(msg);
                else
                {
                    // Chat in camera
                    var room = _rooms.FirstOrDefault(r => r.RoomId == chat.Room);
                    if (room == null) return;
                    room.Player1?.Send(msg);
                    room.Player2?.Send(msg);
                    foreach (var s in room.Spectators) s.Send(msg);
                }
            }
        }

        public List<RoomInfo> GetRoomList()
        {
            lock (_lock)
            {
                return _rooms.Select(r => new RoomInfo
                {
                    RoomId = r.RoomId,
                    HostName = r.Player1?.Username ?? "?",
                    PlayerCount = r.PlayerCount,
                    SpectatorCount = r.Spectators.Count,
                    InProgress = r.InProgress
                }).ToList();
            }
        }

        private void BroadcastRoomList()
        {
            var msg = new NetworkMessage
            {
                Type = MessageType.RoomList,
                Payload = JsonConvert.SerializeObject(
                    new RoomListPayload { Rooms = GetRoomList() })
            };
            foreach (var c in _clients)
                c.Send(msg);
        }
    }
}