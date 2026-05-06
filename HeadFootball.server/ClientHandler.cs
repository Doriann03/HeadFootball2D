using System.Net.Sockets;
using HeadFootball.Shared;
using Newtonsoft.Json;

namespace HeadFootball.Server
{
    public class ClientHandler
    {
        public int UserId { get; private set; } = -1;
        public string Username { get; private set; } = "Unknown";
        public bool IsAuthenticated => UserId != -1;

        private readonly TcpClient _client;
        private readonly StreamWriter _writer;
        private readonly StreamReader _reader;
        private readonly Database _db;
        private readonly LobbyManager _lobby;

        public ClientHandler(TcpClient client, Database db, LobbyManager lobby)
        {
            _client = client;
            _db = db;
            _lobby = lobby;
            _writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            _reader = new StreamReader(client.GetStream());
        }

        // Porneste thread-ul de ascultare pentru acest client
        public void Start()
        {
            new Thread(ReceiveLoop) { IsBackground = true }.Start();
        }

        private void ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    string? line = _reader.ReadLine();
                    if (line == null) break;

                    var msg = JsonConvert.DeserializeObject<NetworkMessage>(line);
                    if (msg == null) continue;

                    HandleMessage(msg);
                }
            }
            catch { }
            finally
            {
                // Clientul s-a deconectat
                Console.WriteLine($"[{Username}] s-a deconectat.");
                _lobby.RemoveClient(this);
                _client.Close();
            }
        }

        private void HandleMessage(NetworkMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.Login:
                    HandleLogin(msg.Payload!);
                    break;

                case MessageType.Register:
                    HandleRegister(msg.Payload!);
                    break;

                case MessageType.CreateRoom:
                    if (IsAuthenticated) _lobby.CreateRoom(this);
                    break;

                case MessageType.JoinRoom:
                    if (IsAuthenticated)
                    {
                        var payload = JsonConvert.DeserializeObject<JoinRoomPayload>(msg.Payload!);
                        if (payload != null)
                            _lobby.JoinRoom(this, payload.RoomId, payload.AsSpectator);
                    }
                    break;

                case MessageType.StatsRequest:
                    HandleStatsRequest();
                    break;

                // ---- AICI ESTE CODUL NOU PENTRU LEADERBOARD ----
                case MessageType.LeaderboardRequest:
                    if (IsAuthenticated)
                    {
                        var leaderboard = _db.GetLeaderboard();
                        Send(new NetworkMessage
                        {
                            Type = MessageType.LeaderboardResponse,
                            Payload = JsonConvert.SerializeObject(leaderboard)
                        });
                    }
                    break;
                // ------------------------------------------------

                case MessageType.ChatMessage:
                    if (IsAuthenticated)
                    {
                        var chat = JsonConvert.DeserializeObject<ChatPayload>(msg.Payload!);
                        if (chat != null)
                        {
                            chat.Sender = Username;
                            _lobby.BroadcastChat(chat);
                        }
                    }
                    break;

                // Input-ul de joc e gestionat direct de GameRoom
                case MessageType.PlayerInput:
                    _lobby.ForwardInput(this, msg);
                    break;
            }
        }

        private void HandleLogin(string payload)
        {
            var data = JsonConvert.DeserializeObject<LoginPayload>(payload);
            if (data == null) return;

            var (success, message, userId) = _db.Login(data.Username, data.Password);

            if (success)
            {
                UserId = userId;
                Username = data.Username;
                Console.WriteLine($"[Login] {Username} conectat.");
            }

            Send(new NetworkMessage
            {
                Type = success ? MessageType.LoginOk : MessageType.LoginFail,
                Payload = JsonConvert.SerializeObject(new LoginResultPayload
                {
                    Success = success,
                    Message = message,
                    PlayerId = userId
                })
            });

            // Dupa login reusit, trimitem lista de camere
            if (success)
                Send(new NetworkMessage
                {
                    Type = MessageType.RoomList,
                    Payload = JsonConvert.SerializeObject(
                        new RoomListPayload { Rooms = _lobby.GetRoomList() })
                });
        }

        private void HandleRegister(string payload)
        {
            var data = JsonConvert.DeserializeObject<RegisterPayload>(payload);
            if (data == null) return;

            var (success, message, userId) = _db.Register(data.Username, data.Password);

            if (success)
            {
                UserId = userId;
                Username = data.Username;
                Console.WriteLine($"[Register] {Username} inregistrat.");
            }

            Send(new NetworkMessage
            {
                Type = success ? MessageType.LoginOk : MessageType.LoginFail,
                Payload = JsonConvert.SerializeObject(new LoginResultPayload
                {
                    Success = success,
                    Message = message,
                    PlayerId = userId
                })
            });

            if (success)
                Send(new NetworkMessage
                {
                    Type = MessageType.RoomList,
                    Payload = JsonConvert.SerializeObject(
                        new RoomListPayload { Rooms = _lobby.GetRoomList() })
                });
        }

        private void HandleStatsRequest()
        {
            var stats = _db.GetStats(Username);
            Send(new NetworkMessage
            {
                Type = MessageType.StatsResponse,
                Payload = JsonConvert.SerializeObject(stats)
            });
        }

        // Trimite un mesaj acestui client
        public void Send(NetworkMessage msg)
        {
            try
            {
                _writer.WriteLine(JsonConvert.SerializeObject(msg));
            }
            catch { }
        }
    }
}