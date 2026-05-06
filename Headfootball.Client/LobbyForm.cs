using HeadFootball.Shared;

namespace Headfootball.Client
{
    public partial class LobbyForm : Form
    {
        private readonly NetworkClient _network;
        private string _currentRoomId = "";

        // Controale UI
        private ListBox _lstRooms = new();
        private Button _btnCreate = new();
        private Button _btnJoin = new();
        private Button _btnSpectate = new();
        private Button _btnStats = new();
        private TextBox _txtChat = new();
        private TextBox _txtChatInput = new();
        private Button _btnSend = new();
        private Label _lblStatus = new();

        public LobbyForm(NetworkClient network)
        {
            _network = network;

            this.Text = "Head Football 2D — Lobby";
            this.ClientSize = new Size(600, 450);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 40);

            BuildUI();
            SubscribeEvents();
        }

        private void BuildUI()
        {
            // Titlu
            var lblTitle = new Label
            {
                Text = "⚽ Lobby",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                Size = new Size(200, 35)
            };

            // Label camere
            var lblRooms = new Label
            {
                Text = "Camere disponibile:",
                ForeColor = Color.LightGray,
                Location = new Point(20, 55),
                Size = new Size(200, 20)
            };

            // Lista camere
            _lstRooms = new ListBox
            {
                Location = new Point(20, 78),
                Size = new Size(340, 200),
                BackColor = Color.FromArgb(30, 30, 50),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9)
            };

            // Butoane camere
            _btnCreate = MakeButton("Creaza Camera", new Point(380, 78),
                Color.DodgerBlue);
            _btnJoin = MakeButton("Intra in Joc", new Point(380, 123),
                Color.SeaGreen);
            _btnSpectate = MakeButton("Spectator", new Point(380, 168),
                Color.DarkOrange);
            _btnStats = MakeButton("Statistici", new Point(380, 213),
                Color.Purple);

            // Status
            _lblStatus = new Label
            {
                Text = "Conectat la lobby.",
                ForeColor = Color.Yellow,
                Location = new Point(20, 285),
                Size = new Size(560, 20),
                Font = new Font("Arial", 9)
            };

            // Chat
            var lblChat = new Label
            {
                Text = "Chat:",
                ForeColor = Color.LightGray,
                Location = new Point(20, 308),
                Size = new Size(50, 20)
            };

            _txtChat = new TextBox
            {
                Location = new Point(20, 328),
                Size = new Size(560, 65),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(30, 30, 50),
                ForeColor = Color.White,
                Font = new Font("Consolas", 8)
            };

            _txtChatInput = new TextBox
            {
                Location = new Point(20, 400),
                Size = new Size(460, 28),
                BackColor = Color.FromArgb(40, 40, 60),
                ForeColor = Color.White
            };
            _txtChatInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) SendChat();
            };

            _btnSend = MakeButton("Trimite", new Point(490, 398), Color.DimGray);
            _btnSend.Size = new Size(90, 30);

            // Evenimente butoane
            _btnCreate.Click += (s, e) => _network.SendCreateRoom();
            _btnJoin.Click += OnJoinClick;
            _btnSpectate.Click += OnSpectateClick;
            _btnStats.Click += (s, e) =>
            {
                var statsForm = new StatsForm(_network);
                statsForm.Show();
            };
            _btnSend.Click += (s, e) => SendChat();

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblRooms, _lstRooms,
                _btnCreate, _btnJoin, _btnSpectate, _btnStats,
                _lblStatus, lblChat, _txtChat,
                _txtChatInput, _btnSend
            });
        }

        private Button MakeButton(string text, Point location, Color color)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(190, 38),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
        }

        private void SubscribeEvents()
        {
            _network.OnRoomList += OnRoomList;
            _network.OnRoomJoined += OnRoomJoined;
            _network.OnPlayerAssigned += OnPlayerAssigned;
            _network.OnChatReceived += OnChatReceived;
        }

        private void OnRoomList(List<RoomInfo> rooms)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                _lstRooms.Items.Clear();
                if (rooms.Count == 0)
                {
                    _lstRooms.Items.Add("Nu exista camere. Creeaza una!");
                    return;
                }
                foreach (var r in rooms)
                {
                    string status = r.InProgress ? "IN JOC" : $"{r.PlayerCount}/2 jucatori";
                    string spectators = r.SpectatorCount > 0
                        ? $" | {r.SpectatorCount} spectatori" : "";
                    _lstRooms.Items.Add(
                        $"[{r.RoomId}] Host: {r.HostName} | {status}{spectators}");
                }
            });
        }

        private void OnRoomJoined(string roomId, bool asSpectator)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                _currentRoomId = roomId;

                if (asSpectator)
                {
                    // Spectatorul sare peste faza de Assigned, îl băgăm direct în meci cu ID 0
                    Console.WriteLine($"DEBUG: Intru ca spectator in camera '{_currentRoomId}'");
                    var gameForm = new MainForm(_network, 0, _currentRoomId);
                    gameForm.Show();
                    this.Hide();
                }
                else
                {
                    _lblStatus.Text = $"Ai intrat in camera {roomId} ca jucator. Asteapta adversarul...";
                    _lblStatus.ForeColor = Color.LightGreen;
                    Console.WriteLine($"DEBUG OnRoomJoined: roomId setat la '{_currentRoomId}'");
                }
            });
        }

        private void OnPlayerAssigned(int playerId)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                Console.WriteLine($"DEBUG OnPlayerAssigned: roomId este '{_currentRoomId}'");
                var gameForm = new MainForm(_network, playerId, _currentRoomId);
                gameForm.Show();
                this.Hide();
            });
        }

        private void OnChatReceived(ChatPayload chat)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                _txtChat.AppendText($"[{chat.Sender}]: {chat.Message}\r\n");
            });
        }

        private void OnJoinClick(object? sender, EventArgs e)
        {
            var selected = _lstRooms.SelectedItem?.ToString();
            if (selected == null || selected.StartsWith("Nu exista"))
            {
                _lblStatus.Text = "Selecteaza o camera din lista!";
                _lblStatus.ForeColor = Color.Tomato;
                return;
            }
            // Extragem RoomId din string-ul "[abc12345] Host: ..."
            string roomId = selected.Substring(1, 8);
            _network.SendJoinRoom(roomId, false);
        }

        private void OnSpectateClick(object? sender, EventArgs e)
        {
            var selected = _lstRooms.SelectedItem?.ToString();
            if (selected == null || selected.StartsWith("Nu exista"))
            {
                _lblStatus.Text = "Selecteaza o camera din lista!";
                _lblStatus.ForeColor = Color.Tomato;
                return;
            }
            string roomId = selected.Substring(1, 8);
            _network.SendJoinRoom(roomId, true);
        }

        private void SendChat()
        {
            if (string.IsNullOrWhiteSpace(_txtChatInput.Text)) return;
            _network.SendChat(_txtChatInput.Text.Trim(), "lobby");
            _txtChatInput.Clear();
        }
    }
}