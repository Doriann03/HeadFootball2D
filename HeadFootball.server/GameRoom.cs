using HeadFootball.Shared;
using Newtonsoft.Json;

namespace HeadFootball.Server
{
    public class GameRoom
    {
        private readonly ClientHandler _player1;
        private readonly ClientHandler _player2;
        private readonly List<ClientHandler> _spectators;
        private readonly Database _db;

        private readonly GameState _state = new();
        private readonly PhysicsEngine _physics = new();
        private PlayerInput _input1 = new() { PlayerId = 1 };
        private PlayerInput _input2 = new() { PlayerId = 2 };
        private bool _running = false;

        public GameRoom(ClientHandler player1, ClientHandler player2,
                        List<ClientHandler> spectators, Database db)
        {
            _player1 = player1;
            _player2 = player2;
            _spectators = spectators;
            _db = db;
        }

        public void Start()
        {
            _player1.Send(new NetworkMessage
            { Type = MessageType.PlayerAssigned, Payload = "1" });
            _player2.Send(new NetworkMessage
            { Type = MessageType.PlayerAssigned, Payload = "2" });

            _running = true;
            _state.GameStarted = true;

            new Thread(GameLoop) { IsBackground = true }.Start();
        }

        public void ReceiveInput(ClientHandler client, NetworkMessage msg)
        {
            var input = JsonConvert.DeserializeObject<PlayerInput>(msg.Payload!);
            if (input == null) return;

            if (client == _player1)
            {
                _input1.Left = input.Left;
                _input1.Right = input.Right;
                _input1.Jump = input.Jump;
                _input1.Kick = input.Kick;
            }
            else if (client == _player2)
            {
                _input2.Left = input.Left;
                _input2.Right = input.Right;
                _input2.Jump = input.Jump;
                _input2.Kick = input.Kick;
            }
        }

        private void GameLoop()
        {
            int totalSeconds = 90;
            var lastSecondTick = DateTime.Now;

            while (_running && totalSeconds > 0)
            {
                Thread.Sleep(16);

                int goal = _physics.Update(_state, _input1, _input2);
                if (goal == 1) _state.Score1++;
                if (goal == 2) _state.Score2++;

                var now = DateTime.Now;
                if ((now - lastSecondTick).TotalMilliseconds >= 1000)
                {
                    totalSeconds--;
                    _state.TimeLeft = totalSeconds;
                    lastSecondTick = now;
                }

                SendAll(new NetworkMessage
                {
                    Type = MessageType.GameState,
                    Payload = JsonConvert.SerializeObject(_state)
                });
            }

            _state.GameOver = true;
            SendAll(new NetworkMessage
            {
                Type = MessageType.GameOver,
                Payload = JsonConvert.SerializeObject(_state)
            });

            // Salvam meciul
            _db.SaveMatch(_player1.UserId, _player2.UserId,
                          _state.Score1, _state.Score2);
            Console.WriteLine($"Meci terminat: {_player1.Username} {_state.Score1}" +
                              $" - {_state.Score2} {_player2.Username}");

            _running = false;
        }

        private void SendAll(NetworkMessage msg)
        {
            _player1.Send(msg);
            _player2.Send(msg);
            foreach (var s in _spectators) s.Send(msg);
        }
    }
}