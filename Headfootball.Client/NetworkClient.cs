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
        public event Action<GameState>? OnStateReceived;
        public event Action<int>? OnPlayerAssigned;
        public event Action<GameState>? OnGameOver;

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
                        case MessageType.PlayerAssigned:
                            PlayerId = int.Parse(msg.Payload!);
                            OnPlayerAssigned?.Invoke(PlayerId);
                            break;

                        case MessageType.GameState:
                            var state = JsonConvert.DeserializeObject<GameState>(msg.Payload!);
                            if (state != null) OnStateReceived?.Invoke(state);
                            break;

                        case MessageType.GameOver:
                            var endState = JsonConvert.DeserializeObject<GameState>(msg.Payload!);
                            if (endState != null) OnGameOver?.Invoke(endState);
                            break;
                    }
                }
                catch { break; }
            }
        }

        public void SendInput(PlayerInput input)
        {
            try
            {
                var msg = new NetworkMessage
                {
                    Type = MessageType.PlayerInput,
                    Payload = JsonConvert.SerializeObject(input)
                };
                _writer.WriteLine(JsonConvert.SerializeObject(msg));
            }
            catch { }
        }
    }
}