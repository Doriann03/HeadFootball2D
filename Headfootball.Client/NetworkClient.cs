using System.Net.Sockets;
using HeadFootball.Shared;
using Newtonsoft.Json;

namespace Headfootball.Client
{
    public class NetworkClient
    {
        private TcpClient _client = new();
        private StreamWriter _writer = null!;
        private StreamReader _reader = null!;

        public int PlayerId { get; private set; }

        public event Action<LoginResultPayload>? OnLoginResult;
        public event Action<List<RoomInfo>>? OnRoomList;
        public event Action<string, bool>? OnRoomJoined; // roomId, asSpectator
        public event Action<int>? OnPlayerAssigned;
        public event Action<GameState>? OnStateReceived;
        public event Action<GameState>? OnGameOver;
        public event Action<ChatPayload>? OnChatReceived;
        public event Action<StatsPayload?>? OnStatsReceived;

        public void Connect(string host, int port)
        {
            _client.Connect(host, port);
            _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
            _reader = new StreamReader(_client.GetStream());
            new Thread(ReceiveLoop) { IsBackground = true }.Start();
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                try
                {
                    string? line = _reader.ReadLine();
                    if (line == null) break;

                    var msg = JsonConvert.DeserializeObject<NetworkMessage>(line);
                    if (msg == null) continue;

                    switch (msg.Type)
                    {
                        case MessageType.LoginOk:
                        case MessageType.LoginFail:
                            var loginResult = JsonConvert.DeserializeObject
                                <LoginResultPayload>(msg.Payload!);
                            if (loginResult != null)
                                OnLoginResult?.Invoke(loginResult);
                            break;

                        case MessageType.RoomList:
                            var roomList = JsonConvert.DeserializeObject<RoomListPayload>(msg.Payload!);
                            if (roomList?.Rooms != null)
                                OnRoomList?.Invoke(roomList.Rooms);
                            break;

                        case MessageType.RoomJoined:
                            var joinPayload = JsonConvert.DeserializeObject
                                <JoinRoomPayload>(msg.Payload!);
                            if (joinPayload != null)
                                OnRoomJoined?.Invoke(joinPayload.RoomId,
                                    joinPayload.AsSpectator);
                            break;

                        case MessageType.PlayerAssigned:
                            PlayerId = int.Parse(msg.Payload!);
                            OnPlayerAssigned?.Invoke(PlayerId);
                            break;

                        case MessageType.GameState:
                            var state = JsonConvert.DeserializeObject
                                <GameState>(msg.Payload!);
                            if (state != null) OnStateReceived?.Invoke(state);
                            break;

                        case MessageType.GameOver:
                            var endState = JsonConvert.DeserializeObject
                                <GameState>(msg.Payload!);
                            if (endState != null) OnGameOver?.Invoke(endState);
                            break;

                        case MessageType.ChatMessage:
                            var chat = JsonConvert.DeserializeObject
                                <ChatPayload>(msg.Payload!);
                            if (chat != null) OnChatReceived?.Invoke(chat);
                            break;

                        case MessageType.StatsResponse:
                            var stats = JsonConvert.DeserializeObject
                                <StatsPayload>(msg.Payload!);
                            OnStatsReceived?.Invoke(stats);
                            break;
                    }
                }
                catch { break; }
            }
        }

        public void SendLogin(string username, string password)
        {
            Send(new NetworkMessage
            {
                Type = MessageType.Login,
                Payload = JsonConvert.SerializeObject(new LoginPayload
                { Username = username, Password = password })
            });
        }

        public void SendRegister(string username, string password)
        {
            Send(new NetworkMessage
            {
                Type = MessageType.Register,
                Payload = JsonConvert.SerializeObject(new RegisterPayload
                { Username = username, Password = password })
            });
        }

        public void SendCreateRoom()
        {
            Send(new NetworkMessage { Type = MessageType.CreateRoom });
        }

        public void SendJoinRoom(string roomId, bool asSpectator = false)
        {
            Send(new NetworkMessage
            {
                Type = MessageType.JoinRoom,
                Payload = JsonConvert.SerializeObject(new JoinRoomPayload
                { RoomId = roomId, AsSpectator = asSpectator })
            });
        }

        public void SendChat(string message, string room = "lobby")
        {
            Send(new NetworkMessage
            {
                Type = MessageType.ChatMessage,
                Payload = JsonConvert.SerializeObject(new ChatPayload
                { Sender = "", Message = message, Room = room })
            });
        }

        public void SendInput(PlayerInput input)
        {
            Send(new NetworkMessage
            {
                Type = MessageType.PlayerInput,
                Payload = JsonConvert.SerializeObject(input)
            });
        }

        public void RequestStats()
        {
            Send(new NetworkMessage { Type = MessageType.StatsRequest });
        }

        private void Send(NetworkMessage msg)
        {
            try { _writer.WriteLine(JsonConvert.SerializeObject(msg)); }
            catch { }
        }
    }
}