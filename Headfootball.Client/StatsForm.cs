using HeadFootball.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Headfootball.Client
{
    public partial class StatsForm : Form
    {
        private readonly NetworkClient _network;
        private TabControl _tabs = new();
        private Panel _myStatsPanel = new();
        private ListView _leaderboardList = new();

        // Labeluri pentru statisticile mele
        private Label _lblUsername = new();
        private Label _lblWins = new();
        private Label _lblLosses = new();
        private Label _lblDraws = new();
        private Label _lblGoals = new();
        private Label _lblRating = new();

        public StatsForm(NetworkClient network)
        {
            _network = network;

            this.Text = "Statistici — Head Football 2D";
            this.ClientSize = new Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 40);

            BuildUI();

            _network.OnStatsReceived += OnStatsReceived;
            _network.OnLeaderboardReceived += OnLeaderboardReceived;

            // Cere datele automat la deschidere
            _network.RequestStats();
            _network.RequestLeaderboard();
        }

        private void BuildUI()
        {
            // Titlu
            var lblTitle = new Label
            {
                Text = "📊 Statistici",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                Size = new Size(470, 30)
            };
            this.Controls.Add(lblTitle);

            // TabControl
            _tabs = new TabControl
            {
                Location = new Point(10, 45),
                Size = new Size(475, 340),
                Font = new Font("Arial", 10)
            };

            // Tab 1 — Statisticile mele
            var tab1 = new TabPage("Statisticile mele")
            {
                BackColor = Color.FromArgb(30, 30, 50)
            };
            BuildMyStatsTab(tab1);
            _tabs.TabPages.Add(tab1);

            // Tab 2 — Leaderboard
            var tab2 = new TabPage("🏆 Leaderboard")
            {
                BackColor = Color.FromArgb(30, 30, 50)
            };
            BuildLeaderboardTab(tab2);
            _tabs.TabPages.Add(tab2);

            // Schimba tab -> cere datele potrivite
            _tabs.SelectedIndexChanged += (s, e) =>
            {
                if (_tabs.SelectedIndex == 0) _network.RequestStats();
                else _network.RequestLeaderboard();
            };

            this.Controls.Add(_tabs);
        }

        private void BuildMyStatsTab(TabPage tab)
        {
            var loading = new Label
            {
                Text = "Se încarcă...",
                ForeColor = Color.Gray,
                Font = new Font("Arial", 10),
                Location = new Point(10, 10),
                Size = new Size(440, 25),
                Name = "lblLoading"
            };
            tab.Controls.Add(loading);

            // Statistici
            int y = 45;
            _lblUsername = MakeStat(tab, "Jucător:", "", y); y += 40;
            _lblRating = MakeStat(tab, "Rating:", "", y); y += 40;
            _lblWins = MakeStat(tab, "Victorii:", "", y); y += 40;
            _lblLosses = MakeStat(tab, "Înfrângeri:", "", y); y += 40;
            _lblDraws = MakeStat(tab, "Egaluri:", "", y); y += 40;
            _lblGoals = MakeStat(tab, "Goluri:", "", y);
        }

        private Label MakeStat(TabPage tab, string labelText, string value, int y)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = Color.LightGray,
                Font = new Font("Arial", 10),
                Location = new Point(20, y),
                Size = new Size(150, 25)
            };
            var val = new Label
            {
                Text = value,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold),
                Location = new Point(180, y),
                Size = new Size(250, 25)
            };
            tab.Controls.Add(lbl);
            tab.Controls.Add(val);
            return val;
        }

        private void BuildLeaderboardTab(TabPage tab)
        {
            _leaderboardList = new ListView
            {
                Location = new Point(5, 5),
                Size = new Size(455, 295),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(30, 30, 50),
                ForeColor = Color.White,
                Font = new Font("Arial", 9)
            };

            _leaderboardList.Columns.Add("#", 35);
            _leaderboardList.Columns.Add("Jucător", 130);
            _leaderboardList.Columns.Add("Rating", 70);
            _leaderboardList.Columns.Add("V", 50);
            _leaderboardList.Columns.Add("Î", 50);
            _leaderboardList.Columns.Add("E", 50);
            _leaderboardList.Columns.Add("Goluri", 70);

            tab.Controls.Add(_leaderboardList);
        }

        private void OnStatsReceived(StatsPayload? stats)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                // Ascunde loading
                var tab = _tabs.TabPages[0];
                var loading = tab.Controls["lblLoading"];
                if (loading != null) loading.Visible = false;

                if (stats == null)
                {
                    _lblUsername.Text = "N/A";
                    return;
                }

                _lblUsername.Text = stats.Username;
                _lblRating.Text = $"⭐ {stats.Rating}";
                _lblWins.Text = $"✅ {stats.Wins}";
                _lblLosses.Text = $"❌ {stats.Losses}";
                _lblDraws.Text = $"🤝 {stats.Draws}";
                _lblGoals.Text = $"⚽ {stats.GoalsScored} marcate / {stats.GoalsConceded} primite";
            });
        }

        private void OnLeaderboardReceived(List<StatsPayload> list)
        {
            if (!this.IsHandleCreated) return;
            this.BeginInvoke(() =>
            {
                _leaderboardList.Items.Clear();
                int rank = 1;
                foreach (var p in list)
                {
                    var item = new ListViewItem(rank.ToString());
                    item.SubItems.Add(p.Username);
                    item.SubItems.Add(p.Rating.ToString());
                    item.SubItems.Add(p.Wins.ToString());
                    item.SubItems.Add(p.Losses.ToString());
                    item.SubItems.Add(p.Draws.ToString());
                    item.SubItems.Add($"{p.GoalsScored}/{p.GoalsConceded}");

                    // Top 3 cu culori
                    if (rank == 1) item.ForeColor = Color.Gold;
                    else if (rank == 2) item.ForeColor = Color.Silver;
                    else if (rank == 3) item.ForeColor = Color.Peru;

                    _leaderboardList.Items.Add(item);
                    rank++;
                }
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _network.OnStatsReceived -= OnStatsReceived;
            _network.OnLeaderboardReceived -= OnLeaderboardReceived;
            base.OnFormClosed(e);
        }
    }
}
