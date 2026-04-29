using HeadFootball.Shared;

namespace Headfootball.Client
{
    public partial class MainForm : Form
    {
        private NetworkClient _network = new();
        private GameRenderer _renderer = new();
        private GameState _state = new();
        private int _playerId = 0;
        private bool _gameOver = false;

        private bool _keyLeft, _keyRight, _keyJump, _keyKick;

        private System.Windows.Forms.Timer _renderTimer = new();
        private System.Windows.Forms.Timer _inputTimer = new();

        public MainForm()
        {
            //InitializeComponent();

            this.Text = "Head Football 2D";
            this.ClientSize = new Size(700, 400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;

            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            this.KeyPreview = true;

            // Render timer ~60fps
            _renderTimer.Interval = 16;
            _renderTimer.Tick += (s, e) => this.Invalidate();
            _renderTimer.Start();

            // Input timer
            _inputTimer.Interval = 16;
            _inputTimer.Tick += SendInput;
            _inputTimer.Start();

            // Conectare server
            ConnectToServer();
        }

        public MainForm(NetworkClient network, int playerId)
        {
            _network = network;
            _playerId = playerId;

            this.Text = "Head Football 2D";
            this.ClientSize = new Size(700, 400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            this.KeyPreview = true;

            _renderTimer.Interval = 16;
            _renderTimer.Tick += (s, e) => this.Invalidate();
            _renderTimer.Start();

            _inputTimer.Interval = 16;
            _inputTimer.Tick += SendInput;
            _inputTimer.Start();

            _network.OnStateReceived += state => _state = state;
            _network.OnGameOver += state =>
            {
                _state = state;
                _gameOver = true;
                _inputTimer.Stop();
            };
        }
        private void ConnectToServer()
        {
            string host = "10.13.50.153"; // schimba cu IP-ul serverului daca e alt PC

            _network.OnPlayerAssigned += id =>
            {
                _playerId = id;
            };

            _network.OnStateReceived += state =>
            {
                _state = state;
            };

            _network.OnGameOver += state =>
            {
                _state = state;
                _gameOver = true;
                _inputTimer.Stop();
            };

            Task.Run(() =>
            {
                try { _network.Connect(host, 5000); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nu ma pot conecta la server!\n{ex.Message}",
                                    "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void SendInput(object? sender, EventArgs e)
        {
            if (_playerId == 0) return;
            _network.SendInput(new PlayerInput
            {
                PlayerId = _playerId,
                Left = _keyLeft,
                Right = _keyRight,
                Jump = _keyJump,
                Kick = _keyKick
            });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_gameOver)
                _renderer.DrawGameOver(e.Graphics, _state);
            else if (!_state.GameStarted)
                _renderer.DrawWaiting(e.Graphics, _playerId);
            else
                _renderer.Draw(e.Graphics, _state, _playerId);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Jucator 1: WASD + Space
            // Jucator 2: Sageti + Enter
            if (_playerId == 1)
            {
                if (e.KeyCode == Keys.A) _keyLeft = true;
                if (e.KeyCode == Keys.D) _keyRight = true;
                if (e.KeyCode == Keys.W) _keyJump = true;
                if (e.KeyCode == Keys.Space) _keyKick = true;
            }
            else
            {
                if (e.KeyCode == Keys.Left) _keyLeft = true;
                if (e.KeyCode == Keys.Right) _keyRight = true;
                if (e.KeyCode == Keys.Up) _keyJump = true;
                if (e.KeyCode == Keys.Enter) _keyKick = true;
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (_playerId == 1)
            {
                if (e.KeyCode == Keys.A) _keyLeft = false;
                if (e.KeyCode == Keys.D) _keyRight = false;
                if (e.KeyCode == Keys.W) _keyJump = false;
                if (e.KeyCode == Keys.Space) _keyKick = false;
            }
            else
            {
                if (e.KeyCode == Keys.Left) _keyLeft = false;
                if (e.KeyCode == Keys.Right) _keyRight = false;
                if (e.KeyCode == Keys.Up) _keyJump = false;
                if (e.KeyCode == Keys.Enter) _keyKick = false;
            }
        }
    }
}