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

        // --- TEXTURĂ ȘI ROTAȚIE MINGE ---
        private Image? imgBall;
        private float _ballAngle = 0f;
        private float _lastBallX = 350f;

        private List<PointF> _ballTrail = new List<PointF>();
        private int _goalTimer = 0;

        // Metodă nouă pentru a declanșa animația de GOL
        public void TriggerGoal()
        {
            _goalTimer = 30; // Va rula timp de 45 de cadre (aprox. 1.5 secunde)
        }
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
                imgBall = Image.FromFile(Path.Combine(path, "ball.png"));
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

            // --- Desenăm textul de COUNTDOWN (Start Meci) ---
            if (state.IsCountdown)
            {
                // Împărțim la 30 pentru a obține secundele (90 cadre = 3 secunde)
                int sec = (int)Math.Ceiling(state.CountdownTimer / 30.0f);
                string text = sec > 0 ? sec.ToString() : "START!";

                using var countFont = new Font("Impact", 80, FontStyle.Italic);
                SizeF size = g.MeasureString(text, countFont);

                // Galben pentru numere, Verde pentru cuvântul START!
                Brush b = sec > 0 ? Brushes.Yellow : Brushes.Lime;

                // Umbră pentru efect 3D
                g.DrawString(text, countFont, Brushes.Black, (FieldW - size.Width) / 2 + 5, (FieldH - size.Height) / 2 - 60 + 5);
                g.DrawString(text, countFont, b, (FieldW - size.Width) / 2, (FieldH - size.Height) / 2 - 60);
            }

            // Animația de GOL ajustată (mai rapidă)
            if (_goalTimer > 0)
            {
                _goalTimer--;

                // Accelerăm zoom-ul: 30 cadre în loc de 45
                float fontSize = 50 + (30 - _goalTimer) * 1.5f;
                using var font = new Font("Impact", fontSize, FontStyle.Italic);

                // Fade-out mai agresiv: înmulțim cu 15 în loc de 10
                int alpha = Math.Min(255, _goalTimer * 15);
                using var brush = new SolidBrush(Color.FromArgb(alpha, Color.Gold));

                string text = "GOOOOL!";
                SizeF size = g.MeasureString(text, font);

                using var shadowBrush = new SolidBrush(Color.FromArgb(alpha, Color.Black));
                g.DrawString(text, font, shadowBrush, (FieldW - size.Width) / 2 + 5, (FieldH - size.Height) / 2 + 5);
                g.DrawString(text, font, brush, (FieldW - size.Width) / 2, (FieldH - size.Height) / 2);
            }
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

            // 3. Desenăm Gheata (cu rotire!)
            float bootW = 55, bootH = 35;
            float bootX = facingRight ? x + 15 : x - 10;
            float bootY = y + 45;

            if (isKicking)
            {
                var gfxState = g.Save(); // Salvăm starea grafică

                float kickOffset = facingRight ? 20 : -20;
                float rotAngle = facingRight ? -35 : 35; // Rotim gheata cu 35 de grade

                // Mutăm punctul de origine în centrul ghetei
                g.TranslateTransform(bootX + kickOffset + bootW / 2, bootY - 10 + bootH / 2);
                g.RotateTransform(rotAngle);

                // Desenăm centrat (originea e pe mijloc acum)
                g.DrawImage(bootImg, -bootW / 2, -bootH / 2, bootW, bootH);
                g.Restore(gfxState); // Resetăm pentru restul jocului
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
            // --- 1. CALCULĂM ROTAȚIA MINGII ---
            // Aflăm câți pixeli s-a mișcat mingea pe orizontală de la ultimul cadru
            float dx = x - _lastBallX;

            // O rotim proporțional. Dacă dx e pozitiv (merge la dreapta), se rotește în sensul acelor de ceas.
            // Dacă dx e negativ (merge la stânga), se rotește invers!
            _ballAngle += dx * 3f; // 3f este multiplicatorul de viteză pentru rotație

            // Păstrăm unghiul în limite normale (0-360 grade)
            _ballAngle %= 360;
            _lastBallX = x;

            // --- 2. CALCULĂM URMA (TRAIL-UL) ---
            if (_ballTrail.Count > 0)
            {
                PointF lastPos = _ballTrail[_ballTrail.Count - 1];
                float trailDx = x - lastPos.X, trailDy = y - lastPos.Y;
                // Dacă mingea s-a teleportat brusc (resetare după gol), ștergem urma
                if (trailDx * trailDx + trailDy * trailDy > 10000) _ballTrail.Clear();
            }
            _ballTrail.Add(new PointF(x, y));
            if (_ballTrail.Count > 10) _ballTrail.RemoveAt(0);

            // --- 3. DESENĂM URMA DE VITEZĂ ---
            for (int i = 0; i < _ballTrail.Count; i++)
            {
                float trailRadius = (20 * scale) * (i / (float)_ballTrail.Count);
                int alpha = (int)(150 * (i / (float)_ballTrail.Count));
                using var brush = new SolidBrush(Color.FromArgb(alpha, 200, 255, 255));
                g.FillEllipse(brush, _ballTrail[i].X - trailRadius, _ballTrail[i].Y - trailRadius, trailRadius * 2, trailRadius * 2);
            }

            // --- 4. DESENĂM TEXTURA MINGII CU ROTAȚIE ---
            float r = 20 * scale;

            if (imgBall != null)
            {
                var gfxState = g.Save(); // Salvăm starea grafică pentru a nu roti tot terenul!

                // Mutăm "centrul universului" grafic exact pe mijlocul mingii
                g.TranslateTransform(x, y);

                // Rotim universul cu unghiul mingii
                g.RotateTransform(_ballAngle);

                // Desenăm imaginea deasupra centrului (de la -r la +r)
                g.DrawImage(imgBall, -r, -r, r * 2, r * 2);

                g.Restore(gfxState); // Resetăm rotația pentru restul jocului
            }
            else
            {
                // Fallback de siguranță: dacă ai uitat să pui poza, desenăm o minge albă simplă
                g.FillEllipse(Brushes.White, x - r, y - r, r * 2, r * 2);
                using var pen = new Pen(Color.Black, 1.5f);
                g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            }
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