using HeadFootball.Shared;
using System.IO;
namespace Headfootball.Client
{
    public class GamePanel : Panel
    {
        public GamePanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);
            this.UpdateStyles();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            // Spune WinForms sa trateze sagetile si Enter ca input normal
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Space:
                case Keys.D1:      // <-- Tasta 1 sus
                case Keys.D2:      // <-- Tasta 2 sus
                case Keys.D3:      // <-- Tasta 3 sus
                case Keys.NumPad1: // <-- Tasta 1 Numpad
                case Keys.NumPad2: // <-- Tasta 2 Numpad
                case Keys.NumPad3: // <-- Tasta 3 Numpad
                    return true;
            }
            return base.IsInputKey(keyData);
        }
    }

    public partial class MainForm : Form
    {
        private readonly NetworkClient _network;
        private GameRenderer _renderer = new();
        private GameState _state = new();
        private int _playerId = 0;
        private bool _gameOver = false;
        private string _currentRoomId;
        private bool _isSwitchingForm = false;
        private int _lastScore1 = 0;
        private int _lastScore2 = 0;
        private bool _lastKick1 = false;
        private bool _lastKick2 = false;
        private bool _wasGameStarted = false;

        private bool _keyLeft, _keyRight, _keyJump, _keyKick;
        private int _currentEmote = 0;

        private System.Windows.Forms.Timer _renderTimer = new();
        private System.Windows.Forms.Timer _inputTimer = new();

        private TextBox _txtChatLog = new();
        private TextBox _txtChatInput = new();
        private Button _btnSendChat = new();
        private GamePanel _gamePanel = new();

        private int _lastCountdownSec = 4;

        public MainForm(NetworkClient network, int playerId, string roomId = "")
        {
            _network = network;
            _playerId = playerId;
            _currentRoomId = roomId;

            this.Text = "Head Football 2D";
            this.ClientSize = new Size(900, 420);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;
            this.KeyUp += OnKeyUp;
            this.BackColor = Color.FromArgb(20, 20, 40);

            BuildUI();

            _renderTimer.Interval = 30;
            _renderTimer.Tick += (s, e) => _gamePanel.Invalidate();
            _renderTimer.Start();

            _inputTimer.Interval = 16;
            _inputTimer.Tick += SendInput;
            _inputTimer.Start();

            //_network.OnStateReceived += state => _state = state;
            //in loc de linia de mai sus, am pus urmatorul bloc de cod:
            _network.OnStateReceived += state =>
            {
                // Punem DOAR partea de redare audio pe Thread-ul principal (UI Thread)
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(() =>
                    {
                        // 1. Sunet Ambiental
                        if (state.GameStarted && !_wasGameStarted)
                        {
                            AudioPlayer.Play("ambient", true);
                            _wasGameStarted = true;
                        }

                        // 2. Sunet de GOL
                        if (state.Score1 > _lastScore1 || state.Score2 > _lastScore2)
                        {
                            AudioPlayer.Play("goal", false);
                            _renderer.TriggerGoal(); // <-- DECLANȘEAZĂ TEXTUL URIAȘ
                        }

                        // 3. Sunet de ȘUT
                        if (state.BallWasKicked)
                        {
                            AudioPlayer.Play("kick", false);
                        }

                        // 4. SUNETE COUNTDOWN
                        if (state.IsCountdown)
                        {
                            int sec = (int)Math.Ceiling(state.CountdownTimer / 30.0f);
                            if (sec < _lastCountdownSec && sec > 0)
                            {
                                // Folosim sunetul de kick pe post de "Beep" la fiecare secundă
                                AudioPlayer.Play("kick", false);
                                _lastCountdownSec = sec;
                            }
                            else if (sec == 0 && _lastCountdownSec > 0)
                            {
                                // Folosim sunetul de galerie pe post de explozie de Start!
                                AudioPlayer.Play("goal", false);
                                _lastCountdownSec = 0;
                            }
                        }
                        else
                        {
                            _lastCountdownSec = 4; // Reset pentru meciurile viitoare
                        }

                        // Salvăm starea pentru cadru următor
                        _lastScore1 = state.Score1;
                        _lastScore2 = state.Score2;
                        _lastKick1 = state.Player1Kicking;
                        _lastKick2 = state.Player2Kicking;
                    });
                }

                _state = state; // Actualizăm starea jocului pe thread-ul de fundal
            };
            _network.OnGameOver += state =>
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(() =>
                    {
                        AudioPlayer.Stop("ambient"); // OPRIM SUNETUL AICI ÎN SIGURANȚĂ
                    });
                }

                _state = state;
                _gameOver = true;
                _inputTimer.Stop();
                ShowGameOver();
            };
            _network.OnChatReceived += OnChatReceived;

            // Dupa ce s-a construit UI, dam focus pe gamePanel
            this.Shown += (s, e) => _gamePanel.Focus();

            // Preîncărcăm sunetele pentru a evita lag-ul de citire de pe disc
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            AudioPlayer.Load(Path.Combine(path, "ambient.wav"), "ambient");
            AudioPlayer.Load(Path.Combine(path, "goal.wav"), "goal");
            AudioPlayer.Load(Path.Combine(path, "kick.wav"), "kick");
        }

        private void BuildUI()
        {
            _gamePanel = new GamePanel
            {
                Location = new Point(0, 0),
                Size = new Size(700, 400),
                BackColor = Color.Black,
                TabStop = true
            };
            _gamePanel.Paint += OnGamePaint;
            _gamePanel.KeyDown += OnKeyDown;
            _gamePanel.KeyUp += OnKeyUp;
            _gamePanel.MouseClick += (s, e) => _gamePanel.Focus();
            this.Controls.Add(_gamePanel);

            var lblChat = new Label
            {
                Text = "💬 Chat",
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(710, 10),
                Size = new Size(180, 25),
                BackColor = Color.Transparent
            };

            _txtChatLog = new TextBox
            {
                Location = new Point(710, 38),
                Size = new Size(180, 290),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 50),
                ForeColor = Color.White,
                Font = new Font("Consolas", 8),
                WordWrap = true
            };

            _txtChatInput = new TextBox
            {
                Location = new Point(710, 335),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(40, 40, 60),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            _txtChatInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                    SendChat();
                }
            };

            _btnSendChat = new Button
            {
                Text = "Trimite",
                Location = new Point(710, 365),
                Size = new Size(180, 28),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 8, FontStyle.Bold)
            };
            _btnSendChat.Click += (s, e) => SendChat();

            string controlsText = "👁 MOD SPECTATOR - Doar privești";
            if (_playerId == 1) controlsText = "🎮 P1: A/D=miscare\nW=saritura\nSpace=sut";
            else if (_playerId == 2) controlsText = "🎮 P2: ←/→=miscare\n↑=saritura\nEnter=sut";

            var lblControls = new Label
            {
                Text = controlsText,
                ForeColor = Color.LightGray,
                Font = new Font("Arial", 7),
                Location = new Point(710, 395),
                Size = new Size(180, 20),
                BackColor = Color.Transparent
            };

            this.Controls.AddRange(new Control[]
            {
                lblChat, _txtChatLog, _txtChatInput, _btnSendChat, lblControls
            });
        }

        private void SendChat()
        {
            string msg = _txtChatInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(msg)) return;
            _network.SendChat(msg, string.IsNullOrEmpty(_currentRoomId) ? "game" : _currentRoomId);
            _txtChatInput.Clear();
            _gamePanel.Focus();
        }

        private void OnChatReceived(ChatPayload chat)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                _txtChatLog.AppendText($"[{chat.Sender}]: {chat.Message}\r\n");
                _txtChatLog.SelectionStart = _txtChatLog.Text.Length;
                _txtChatLog.ScrollToCaret();
            });
        }

        private void ShowGameOver()
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                string winner = _state.Score1 > _state.Score2 ? "Jucatorul 1 castiga!" :
                                _state.Score2 > _state.Score1 ? "Jucatorul 2 castiga!" :
                                "Egalitate!";

                var result = MessageBox.Show(
                    $"Meci terminat!\n{winner}\nScor: {_state.Score1} - {_state.Score2}\n\nVrei sa te intorci la lobby?",
                    "Game Over", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    _renderTimer.Stop();
                    var lobby = new LobbyForm(_network);
                    lobby.Show();
                    _isSwitchingForm = true;
                    this.Close();
                }
            });
        }

        private void OnGamePaint(object? sender, PaintEventArgs e)
        {
            if (_gameOver)
                _renderer.DrawGameOver(e.Graphics, _state);
            else if (!_state.GameStarted)
                _renderer.DrawWaiting(e.Graphics, _playerId);
            else
                _renderer.Draw(e.Graphics, _state, _playerId);

            // Dacă ești spectator, afișăm un badge subtil pe ecran în timpul meciului
            if (_playerId == 0 && _state.GameStarted && !_gameOver)
            {
                using var font = new Font("Arial", 12, FontStyle.Bold);
                using var brush = new SolidBrush(Color.FromArgb(150, 255, 255, 0)); // Galben semi-transparent
                e.Graphics.DrawString("👁 SPECTATOR", font, brush, 10, 10);
            }
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
                Kick = _keyKick,
                Emote = _currentEmote // Am reparat aici (fără testul cu _keyJump)
            });

            // Lăsăm asta FĂRĂ _currentEmote = 0; aici. Resetarea se face corect în OnKeyUp.
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (_txtChatInput.Focused) return;

            // Capturăm tastele numerice pentru Emotes (1, 2, 3)
            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) _currentEmote = 1;
            if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) _currentEmote = 2;
            if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3) _currentEmote = 3;

            if (_playerId == 1)
            {
                if (e.KeyCode == Keys.A) { _keyLeft = true; e.Handled = true; }
                if (e.KeyCode == Keys.D) { _keyRight = true; e.Handled = true; }
                if (e.KeyCode == Keys.W) { _keyJump = true; e.Handled = true; }
                if (e.KeyCode == Keys.Space) { _keyKick = true; e.Handled = true; }
            }
            else if (_playerId == 2)
            {
                if (e.KeyCode == Keys.Left) { _keyLeft = true; e.Handled = true; }
                if (e.KeyCode == Keys.Right) { _keyRight = true; e.Handled = true; }
                if (e.KeyCode == Keys.Up) { _keyJump = true; e.Handled = true; }
                if (e.KeyCode == Keys.Enter) { _keyKick = true; e.Handled = true; }

                if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1)
                {
                    _currentEmote = 1;
                    Console.WriteLine("DEBUG: Emote 1 detectat!"); // Linia asta îți va arăta în Output dacă tasta merge
                }
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            // Resetăm emote-ul când ridică degetul de pe tasta 1, 2 sau 3
            if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) _currentEmote = 0;
            if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) _currentEmote = 0;
            if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3) _currentEmote = 0;

            if (_playerId == 1)
            {
                if (e.KeyCode == Keys.A) _keyLeft = false;
                if (e.KeyCode == Keys.D) _keyRight = false;
                if (e.KeyCode == Keys.W) _keyJump = false;
                if (e.KeyCode == Keys.Space) _keyKick = false;
            }
            else if (_playerId == 2)
            {
                if (e.KeyCode == Keys.Left) _keyLeft = false;
                if (e.KeyCode == Keys.Right) _keyRight = false;
                if (e.KeyCode == Keys.Up) _keyJump = false;
                if (e.KeyCode == Keys.Enter) _keyKick = false;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (!_isSwitchingForm) // Dacă NU schimbăm fereastra, închidem tot
            {
                Application.Exit();
            }
        }
    }
}