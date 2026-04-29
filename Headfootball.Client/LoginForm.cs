using HeadFootball.Shared;

namespace Headfootball.Client
{
    public partial class LoginForm : Form
    {
        private readonly NetworkClient _network;

        private TextBox _txtUsername = new();
        private TextBox _txtPassword = new();
        private Button _btnLogin = new();
        private Button _btnRegister = new();
        private Label _lblMessage = new();

        public LoginForm(NetworkClient network)
        {
            _network = network;

            this.Text = "Head Football 2D — Login";
            this.ClientSize = new Size(350, 250);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 40);

            // Titlu
            var lblTitle = new Label
            {
                Text = "⚽ Head Football 2D",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(60, 20),
                Size = new Size(240, 35)
            };

            // Username
            var lblUser = new Label
            {
                Text = "Username:",
                ForeColor = Color.LightGray,
                Location = new Point(40, 70),
                Size = new Size(80, 20)
            };
            _txtUsername = new TextBox
            {
                Location = new Point(130, 68),
                Size = new Size(170, 25)
            };

            // Parola
            var lblPass = new Label
            {
                Text = "Parola:",
                ForeColor = Color.LightGray,
                Location = new Point(40, 105),
                Size = new Size(80, 20)
            };
            _txtPassword = new TextBox
            {
                Location = new Point(130, 103),
                Size = new Size(170, 25),
                PasswordChar = '*'
            };

            // Mesaj eroare/succes
            _lblMessage = new Label
            {
                Text = "",
                ForeColor = Color.Tomato,
                Location = new Point(40, 138),
                Size = new Size(270, 20),
                Font = new Font("Arial", 8)
            };

            // Butoane
            _btnLogin = new Button
            {
                Text = "Login",
                Location = new Point(60, 165),
                Size = new Size(100, 35),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            _btnRegister = new Button
            {
                Text = "Register",
                Location = new Point(185, 165),
                Size = new Size(100, 35),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            _btnLogin.Click += OnLoginClick;
            _btnRegister.Click += OnRegisterClick;

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblUser, _txtUsername,
                lblPass, _txtPassword, _lblMessage,
                _btnLogin, _btnRegister
            });

            // Abonare la raspunsul de la server
            _network.OnLoginResult += OnLoginResult;
        }

        private void OnLoginClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtUsername.Text) ||
                string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                _lblMessage.Text = "Completeaza username si parola!";
                return;
            }
            _btnLogin.Enabled = false;
            _btnRegister.Enabled = false;
            _lblMessage.ForeColor = Color.Yellow;
            _lblMessage.Text = "Se conecteaza...";
            _network.SendLogin(_txtUsername.Text.Trim(), _txtPassword.Text);
        }

        private void OnRegisterClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtUsername.Text) ||
                string.IsNullOrWhiteSpace(_txtPassword.Text))
            {
                _lblMessage.Text = "Completeaza username si parola!";
                return;
            }
            _btnLogin.Enabled = false;
            _btnRegister.Enabled = false;
            _lblMessage.ForeColor = Color.Yellow;
            _lblMessage.Text = "Se inregistreaza...";
            _network.SendRegister(_txtUsername.Text.Trim(), _txtPassword.Text);
        }

        private void OnLoginResult(LoginResultPayload result)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                if (result.Success)
                {
                    _network.OnLoginResult -= OnLoginResult;
                    var lobby = new LobbyForm(_network);
                    lobby.Show();
                    this.Hide();
                }
                else
                {
                    _lblMessage.ForeColor = Color.Tomato;
                    _lblMessage.Text = result.Message;
                    _btnLogin.Enabled = true;
                    _btnRegister.Enabled = true;
                }
            });
        }
    }
}