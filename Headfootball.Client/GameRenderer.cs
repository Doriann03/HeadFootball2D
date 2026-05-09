using HeadFootball.Shared;
using System.IO;
namespace Headfootball.Client
{
    
    public class GameRenderer
    {
        private const float FieldW = 700;
        private const float FieldH = 400;
        private const float GroundY = 330;
        private const float GoalH = 160;
        private const float GoalW = 50;
        private Image? imgHead1;
        private Image? imgHead2;
        private Image? imgFoot1;
        private Image? imgFoot2;
        private Image? imgBackground;
        public GameRenderer()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            try
            {
                imgHead1 = Image.FromFile(Path.Combine(path, "head1.png"));
                imgHead2 = Image.FromFile(Path.Combine(path, "head2.png"));
                imgFoot1 = Image.FromFile(Path.Combine(path, "foot1.png")); // Gheata pt P1
                imgFoot2 = Image.FromFile(Path.Combine(path, "foot2.png")); // Gheata pt P2
                imgBackground = Image.FromFile(Path.Combine(path, "bg.jpg"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare la încărcarea resurselor: " + ex.Message);
            }
        }

        public void Draw(Graphics g, GameState state, int playerId)
        {
            // --- OPTIMIZĂRI DE PERFORMANȚĂ PENTRU GDI+ ---
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

            //g.Clear(Color.FromArgb(34, 139, 34));
            if (imgBackground != null)
            {
                g.DrawImage(imgBackground, 0, 0, FieldW, FieldH);
            }
            else
            {
                g.Clear(Color.FromArgb(34, 139, 34)); // Fallback dacă lipsește imaginea
            }

         

            DrawGoals(g);

            // Power-up pe teren
            if (state.PowerUpVisible)
                DrawPowerUp(g, state.PowerUpX, state.PowerUpY, state.PowerUpType);

            // Desenăm Jucătorii (fără culori, doar datele necesare)
            // Înlocuiește apelurile vechi de DrawPlayer cu acestea:
            DrawPlayer(g, state.Player1X, state.Player1Y, "P1", playerId == 1, state.Player1Kicking, true, state.Player1Emote, state.Player1EmoteTimer);
            DrawPlayer(g, state.Player2X, state.Player2Y, "P2", playerId == 2, state.Player2Kicking, false, state.Player2Emote, state.Player2EmoteTimer);

            DrawBall(g, state.BallX, state.BallY, state.BallScale);
            DrawPowerUpIndicators(g, state);
            DrawHUD(g, state);
        }

        private void DrawPlayer(Graphics g, float x, float y, string label, bool isYou, bool isKicking, bool facingRight, int emote, int emoteTimer)
        {
            const float pw = 40;
            float headDisplaySize = 70;

            // 1. Alegem imaginea corectă pe baza direcției (P1=true, P2=false)
            Image imgToDraw = facingRight ? imgHead2! : imgHead1!;
            Image bootImg = facingRight ? imgFoot2! : imgFoot1!;

            // 2. Desenăm Capul
            g.DrawImage(imgToDraw, x - (headDisplaySize - pw) / 2, y, headDisplaySize, headDisplaySize);

            // 3. Desenăm Gheata
            float bootW = 55, bootH = 35;
            float bootX = facingRight ? x + 15 : x - 10;
            float bootY = y + 45;

            if (isKicking)
            {
                float kickOffset = facingRight ? 25 : -25;
                g.DrawImage(bootImg, bootX + kickOffset, bootY - 10, bootW, bootH);
            }
            else
            {
                g.DrawImage(bootImg, bootX, bootY, bootW, bootH);
            }

            // 4. Desenăm eticheta
            string displayLabel = isYou ? "YOU" : label;
            using var font = new Font("Arial", 8, FontStyle.Bold);
            using var labelBrush = new SolidBrush(isYou ? Color.Yellow : Color.White);
            g.DrawString(displayLabel, font, labelBrush, x + 5, y - 20);

            // 5. Desenăm Emote-ul (dacă timerul e activ)
            if (emoteTimer > 0 && emote > 0)
            {
                // Am înlocuit emoji-urile invizibile cu text
                string emojiText = emote switch { 1 => "HAHA", 2 => "GRRR", 3 => "YAY!", _ => "..." };

                using var emoteFont = new Font("Impact", 10);

                float bubbleX = x + 15;
                float bubbleY = y - 35;

                // Desenăm balonul de dialog (l-am făcut puțin mai oval pentru text)
                g.FillEllipse(Brushes.WhiteSmoke, bubbleX, bubbleY, 45, 25);
                g.DrawEllipse(Pens.LightGray, bubbleX, bubbleY, 45, 25);

                // Centram textul in balon
                g.DrawString(emojiText, emoteFont, Brushes.Black, bubbleX + 5, bubbleY + 4);
            }
        }

        private void DrawBall(Graphics g, float x, float y, float scale)
        {
            float r = 20 * scale;
            g.FillEllipse(Brushes.White, x - r, y - r, r * 2, r * 2);
            using var pen = new Pen(Color.Black, 1.5f);
            g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            g.DrawLine(pen, x, y - r, x, y + r);
            g.DrawLine(pen, x - r, y, x + r, y);
        }

        private void DrawPowerUp(Graphics g, float x, float y, int type)
        {
            Color[] colors = { Color.Transparent, Color.Yellow, Color.Cyan, Color.Orange, Color.Magenta };
            string[] icons = { "", "V", "S", "B", "2x" };

            if (type < 1 || type > 4) return;

            using var brush = new SolidBrush(colors[type]);
            using var pen = new Pen(Color.White, 2);
            g.FillEllipse(brush, x - 12, y - 12, 25, 25);
            g.DrawEllipse(pen, x - 12, y - 12, 25, 25);

            using var font = new Font("Arial", 7, FontStyle.Bold);
            g.DrawString(icons[type], font, Brushes.Black, x - 6, y - 7);
        }

        private void DrawPowerUpIndicators(Graphics g, GameState state)
        {
            string[] names = { "", "VITEZA", "SARITURA", "MINGE", "GOL x2" };
            Color[] colors = { Color.Transparent, Color.Yellow, Color.Cyan, Color.Orange, Color.Magenta };

            if (state.Player1ActivePowerUp > 0 && state.Player1ActivePowerUp <= 4)
            {
                int type = state.Player1ActivePowerUp;
                using var brush = new SolidBrush(colors[type]);
                using var font = new Font("Arial", 8, FontStyle.Bold);
                g.FillRectangle(brush, 5, 5, 90, 20);
                g.DrawString($"P1: {names[type]}", font, Brushes.Black, 7, 7);
            }

            if (state.Player2ActivePowerUp > 0 && state.Player2ActivePowerUp <= 4)
            {
                int type = state.Player2ActivePowerUp;
                using var brush = new SolidBrush(colors[type]);
                using var font = new Font("Arial", 8, FontStyle.Bold);
                g.FillRectangle(brush, FieldW - 95, 5, 90, 20);
                g.DrawString($"P2: {names[type]}", font, Brushes.Black, FieldW - 93, 7);
            }
        }

        private void DrawHUD(Graphics g, GameState state)
        {
            float centerX = FieldW / 2; // Mijlocul terenului (350)

            // 1. Fundal pentru scor (opțional, pentru contrast)
            using var hudBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            g.FillRectangle(hudBrush, centerX - 80, 5, 160, 50);

            // 2. Scorul - folosim un font mai mare și "Impact" pentru aspect sportiv
            using var scorFont = new Font("Impact", 28, FontStyle.Regular);
            string scoreText = $"{state.Score1} : {state.Score2}";

            // Calculăm dimensiunea exactă a textului pentru centrare
            SizeF scoreSize = g.MeasureString(scoreText, scorFont);
            g.DrawString(scoreText, scorFont, Brushes.White, centerX - (scoreSize.Width / 2), 5);

            // 3. Timerul - îl punem fix sub scor
            using var timeFont = new Font("Segoe UI", 16, FontStyle.Bold); // Am crescut fontul de la 12 la 16
            string timeText = $"⏱ {state.TimeLeft}s";

            SizeF timeSize = g.MeasureString(timeText, timeFont);
            // Timerul devine roșu sub 10 secunde
            Brush timeColor = state.TimeLeft <= 10 ? Brushes.Red : Brushes.Yellow;

            // Am coborât poziția pe verticală de la 42 la 55
            g.DrawString(timeText, timeFont, timeColor, centerX - (timeSize.Width / 2), 55);
        }

        public void DrawWaiting(Graphics g, int playerId)
        {
            g.Clear(Color.FromArgb(20, 20, 40));
            using var font = new Font("Arial", 16, FontStyle.Bold);

            string msg = playerId == 0
                ? "👁 Meciul nu a început încă. Așteptăm jucătorii..."
                : $"Esti Jucatorul {playerId}. Astept adversarul...";

            g.DrawString(msg, font, Brushes.White, 150, 180);
        }

        public void DrawGameOver(Graphics g, GameState state)
        {
            g.Clear(Color.FromArgb(20, 20, 40));
            using var font = new Font("Arial", 24, FontStyle.Bold);
            using var smallFont = new Font("Arial", 14);

            string winner = state.Score1 > state.Score2 ? "Jucatorul 1 castiga!" :
                            state.Score2 > state.Score1 ? "Jucatorul 2 castiga!" :
                            "Egalitate!";

            g.DrawString("MECI TERMINAT", font, Brushes.Yellow, 200, 150);
            g.DrawString(winner, font, Brushes.White, 210, 200);
            g.DrawString($"Scor final: {state.Score1} - {state.Score2}",
                         smallFont, Brushes.LightGray, 260, 260);
        }

        private static void DrawGoals(Graphics g)
        {
            float frameThickness = 8f; // Grosimea barei
            using var frameBrush = new SolidBrush(Color.LightSteelBlue); // Culoarea barelor
            using var frameBorderPen = new Pen(Color.DarkSlateGray, 2);  // Conturul barelor
            using var netPen = new Pen(Color.WhiteSmoke, 2); // Plasa albă

            // Funcție internă pentru a desena o singură poartă
            void DrawSingleGoal(float xStart, bool isLeft)
            {
                // --- 1. Desenăm Plasa (Romburi) ---
                // Definim zona (dreptunghiul) în care are voie să fie desenată plasa
                RectangleF goalRect = new RectangleF(isLeft ? 0 : xStart, GroundY - GoalH, GoalW, GoalH);

                // Blocăm desenarea în afara acestui dreptunghi
                g.SetClip(goalRect);

                int spacing = 12; // Cât de deasă e plasa
                                  // Desenăm linii diagonale
                for (int i = -300; i < 300; i += spacing)
                {
                    g.DrawLine(netPen, goalRect.Left, goalRect.Top + i, goalRect.Right, goalRect.Top + i + goalRect.Width);
                    g.DrawLine(netPen, goalRect.Left, goalRect.Bottom - i, goalRect.Right, goalRect.Bottom - i - goalRect.Width);
                }
                g.ResetClip(); // Scoatem limitarea pentru a desena restul elementelor normal

                // --- 2. Desenăm Cadrul metalic (Barele) ---

                // Bara din spate (pentru efect de adâncime 3D ca în poză)
                float backPostX = isLeft ? 0 : xStart + GoalW - frameThickness;
                RectangleF backPost = new RectangleF(backPostX, GroundY - GoalH, frameThickness, GoalH);
                g.FillRectangle(frameBrush, backPost);
                g.DrawRectangle(frameBorderPen, backPost.X, backPost.Y, backPost.Width, backPost.Height);

                // Bara transversală (de sus)
                RectangleF crossbar = new RectangleF(isLeft ? 0 : xStart, GroundY - GoalH, GoalW, frameThickness);
                g.FillRectangle(frameBrush, crossbar);
                g.DrawRectangle(frameBorderPen, crossbar.X, crossbar.Y, crossbar.Width, crossbar.Height);

                // Bara verticală din față (stâlpul pe care îl lovește mingea)
                float frontPostX = isLeft ? GoalW - frameThickness : xStart;
                RectangleF frontPost = new RectangleF(frontPostX, GroundY - GoalH, frameThickness, GoalH);
                g.FillRectangle(frameBrush, frontPost);
                g.DrawRectangle(frameBorderPen, frontPost.X, frontPost.Y, frontPost.Width, frontPost.Height);
            }

            // Apelăm funcția pentru a desena poarta din Stânga
            DrawSingleGoal(0, true);

            // Apelăm funcția pentru a desena poarta din Dreapta
            float rightGoalX = FieldW - GoalW;
            DrawSingleGoal(rightGoalX, false);
        }
    }
}