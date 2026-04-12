using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using HeadFootball.Shared;
using Newtonsoft.Json;

namespace HeadFootball.Server
{
    public class GameRoom
    {
        private TcpClient _client1, _client2;
        private GameState _state = new();
        private PhysicsEngine _physics = new();
        private PlayerInput _input1 = new() { PlayerId = 1 };
        private PlayerInput _input2 = new() { PlayerId = 2 };
        private bool _running = false;

        public GameRoom(TcpClient client1, TcpClient client2)
        {
            _client1 = client1;
            _client2 = client2;
        }

        public void Start()
        {
            AssignPlayers();
            _running = true;
            _state.GameStarted = true;

            // Thread-uri pentru input
            new Thread(() => ReceiveInput(_client1, _input1)).Start();
            new Thread(() => ReceiveInput(_client2, _input2)).Start();

            // Game loop
            new Thread(GameLoop).Start();
        }

        private void AssignPlayers()
        {
            Send(_client1, new NetworkMessage
            {
                Type = MessageType.PlayerAssigned,
                Payload = "1"
            });
            Send(_client2, new NetworkMessage
            {
                Type = MessageType.PlayerAssigned,
                Payload = "2"
            });
        }

        private void GameLoop()
        {
            int totalSeconds = 90;
            var lastTick = DateTime.Now;

            while (_running && totalSeconds > 0)
            {
                Thread.Sleep(16); // ~60 FPS

                _physics.Update(_state, _input1, _input2);

                int goal = _physics.CheckGoal(_state);
                if (goal == 1) _state.Score1++;
                if (goal == 2) _state.Score2++;

                // Cronometru
                if ((DateTime.Now - lastTick).TotalSeconds >= 1)
                {
                    totalSeconds--;
                    _state.TimeLeft = totalSeconds;
                    lastTick = DateTime.Now;
                }

                // Trimite starea la ambii clienti
                var msg = new NetworkMessage
                {
                    Type = MessageType.GameState,
                    Payload = JsonConvert.SerializeObject(_state)
                };
                Send(_client1, msg);
                Send(_client2, msg);
            }

            _state.GameOver = true;
            var endMsg = new NetworkMessage
            {
                Type = MessageType.GameOver,
                Payload = JsonConvert.SerializeObject(_state)
            };
            Send(_client1, endMsg);
            Send(_client2, endMsg);

            _running = false;
        }

        private void ReceiveInput(TcpClient client, PlayerInput input)
        {
            var reader = new StreamReader(client.GetStream());
            while (_running)
            {
                try
                {
                    string? line = reader.ReadLine();
                    if (line == null) break;

                    var msg = JsonConvert.DeserializeObject<NetworkMessage>(line);
                    if (msg?.Type == MessageType.PlayerInput)
                    {
                        var received = JsonConvert.DeserializeObject<PlayerInput>(msg.Payload!);
                        if (received != null)
                        {
                            input.Left = received.Left;
                            input.Right = received.Right;
                            input.Jump = received.Jump;
                            input.Kick = received.Kick;
                        }
                    }
                }
                catch { break; }
            }
        }

        private void Send(TcpClient client, NetworkMessage msg)
        {
            try
            {
                var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                writer.WriteLine(JsonConvert.SerializeObject(msg));
            }
            catch { }
        }
    }
}
