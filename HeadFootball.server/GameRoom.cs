using System.Net.Sockets;
using HeadFootball.Shared;
using Newtonsoft.Json;

namespace HeadFootball.Server
{
    public class GameRoom
    {
        private readonly ClientHandler _player1;
        private readonly ClientHandler? _player2; // Acum poate fi null (daca e bot)
        private readonly List<ClientHandler> _spectators;
        private readonly Database _db;
        private readonly PhysicsEngine _physics = new();
        private GameState _state = new();
        private PlayerInput _input1 = new() { PlayerId = 1 };
        private PlayerInput _input2 = new() { PlayerId = 2 };
        private bool _running = false;

        public GameRoom(ClientHandler player1, ClientHandler? player2,
                        List<ClientHandler> spectators, Database db)
        {
            _player1 = player1;
            _player2 = player2;
            _spectators = spectators;
            _db = db;
        }

        public void Start()
        {
            _state.GameStarted = true;
            _running = true;

            _player1.Send(new NetworkMessage
            {
                Type = MessageType.PlayerAssigned,
                Payload = "1"
            });

            // Daca avem jucator real 2, ii trimitem mesajul
            _player2?.Send(new NetworkMessage
            {
                Type = MessageType.PlayerAssigned,
                Payload = "2"
            });

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
                _input1.Emote = input.Emote; // <-- PIESA LIPSĂ PENTRU P1!
            }
            else if (_player2 != null && client == _player2)
            {
                _input2.Left = input.Left;
                _input2.Right = input.Right;
                _input2.Jump = input.Jump;
                _input2.Kick = input.Kick;
                _input2.Emote = input.Emote; // <-- PIESA LIPSĂ PENTRU P2!
            }
        }

        private void GameLoop()
        {
            int totalSeconds = 90;
            var lastSecondTick = DateTime.Now;

            while (_running && totalSeconds > 0)
            {
                Thread.Sleep(16);

                // --- AI BOT LOGIC ---
                // Daca Player 2 este null, inseamna ca jucam cu botul.
                if (_player2 == null)
                {
                    SimulateBotInput();
                }
                // --------------------

                _physics.Update(_state, _input1, _input2);

                int goal = _physics.CheckGoal(_state);
                if (goal == 11) _state.Score1 += 1;
                if (goal == 12) _state.Score1 += 2;
                if (goal == 21) _state.Score2 += 1;
                if (goal == 22) _state.Score2 += 2;

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
            var endMsg = new NetworkMessage
            {
                Type = MessageType.GameOver,
                Payload = JsonConvert.SerializeObject(_state)
            };
            SendAll(endMsg);

            // Salvam doar daca P2 e jucator real, altfel ignoram ca sa nu stricam ELO-ul pentru bot
            if (_player2 != null)
            {
                _db.SaveMatch(_player1.UserId, _player2.UserId, _state.Score1, _state.Score2);
            }

            _running = false;
        }

        // Functia care ii da viata botului
        private void SimulateBotInput()
        {
            _input2.Left = false;
            _input2.Right = false;
            _input2.Jump = false;
            _input2.Kick = false;

            // Miscarea stanga-dreapta (urmareste mingea, dar sta in jumatatea lui)
            float targetX = _state.BallX;
            if (targetX < PhysicsEngine.FieldWidth / 2)
                targetX = PhysicsEngine.FieldWidth / 2 + 50; // Nu prea se baga peste P1

            if (_state.Player2X > targetX + 10) _input2.Left = true;
            else if (_state.Player2X < targetX - 10) _input2.Right = true;

            // Saritura (daca mingea vine sus si e aproape)
            float distY = _state.Player2Y - _state.BallY;
            float distX = Math.Abs(_state.Player2X - _state.BallX);

            if (distY > 20 && distX < 80)
            {
                _input2.Jump = true;
            }

            // Sut (daca mingea e foarte aproape de el)
            if (distX < 60 && Math.Abs(_state.BallY - _state.Player2Y) < 60)
            {
                _input2.Kick = true;
            }
        }

        private void SendAll(NetworkMessage msg)
        {
            _player1.Send(msg);
            _player2?.Send(msg); // ? previne eroare daca e null
            foreach (var s in _spectators)
                s.Send(msg);
        }
    }
}