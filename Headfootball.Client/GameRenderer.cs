using HeadFootball.Shared;

namespace Headfootball.Client
{
    public class GameRenderer
    {
        private const float FieldW = 700;
        private const float FieldH = 400;
        private const float GroundY = 330;
        private const float GoalH = 80;
        private const float GoalW = 20;

        public void Draw(Graphics g, GameState state, int playerId)
        {
            // Fundal - teren verde
            g.Clear(Color.FromArgb(34, 139, 34));

            // Linie de mijloc
            using var whitePen = new Pen(Color.White, 2);
            g.DrawLine(whitePen, FieldW / 2, 0, FieldW / 2, GroundY);

            // Cerc de mijloc
            g.DrawEllipse(whitePen, FieldW / 2 - 50, GroundY / 2 - 50, 100, 100);

            // Sol
            using var groundBrush = new SolidBrush(Color.SaddleBrown);
            g.FillRectangle(groundBrush, 0, GroundY, FieldW, FieldH - GroundY);

            // Poarta stanga (Player2 apara)
            using var goalBrush = new SolidBrush(Color.White);
            using var goalPen = new Pen(Color.Gray, 3);
            g.FillRectangle(goalBrush, 0, GroundY - GoalH, GoalW, GoalH);
            g.DrawRectangle(goalPen, 0, GroundY - GoalH, GoalW, GoalH);

            // Poarta dreapta (Player1 apara)
            g.FillRectangle(goalBrush, FieldW - GoalW, GroundY - GoalH, GoalW, GoalH);
            g.DrawRectangle(goalPen, FieldW - GoalW, GroundY - GoalH, GoalW, GoalH);

            // Jucator 1 (albastru) - fata spre dreapta
            DrawPlayer(g, state.Player1X, state.Player1Y, Color.DodgerBlue,
                       Color.DarkBlue, "P1", playerId == 1,
                       isKicking: state.Player1Kicking, facingRight: true);

            // Jucator 2 (rosu) - fata spre stanga
            DrawPlayer(g, state.Player2X, state.Player2Y, Color.Tomato,
                       Color.DarkRed, "P2", playerId == 2,
                       isKicking: state.Player2Kicking, facingRight: false);

            // Minge
            DrawBall(g, state.BallX, state.BallY);

            // HUD - scor si timp
            DrawHUD(g, state);
        }

        private void DrawPlayer(Graphics g, float x, float y,
                          Color bodyColor, Color headColor,
                          string label, bool isYou, bool isKicking, bool facingRight)
        {
            const float pw = 40, ph = 60;
            const float headR = 22;

            // Corp
            using var bodyBrush = new SolidBrush(bodyColor);
            g.FillRectangle(bodyBrush, x, y + ph / 2, pw, ph / 2);

            // Picior (gheata)
            using var bootBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            using var bootPen = new Pen(Color.Black, 1.5f);

            if (isKicking)
            {
                // Picior ridicat la sut (usor in fata)
                float kickX = facingRight ? x + pw - 2 : x - 22;
                float kickY = y + ph / 2 + 2;
                g.FillEllipse(bootBrush, kickX, kickY, 24, 12);
                g.DrawEllipse(bootPen, kickX, kickY, 24, 12);
            }
            else
            {
                // Picior jos normal - facem piciorul din fata sa iasa putin in fata corpului
                if (facingRight)
                {
                    // picior din fata (dreapta) avansat usor
                    g.FillEllipse(bootBrush, x + 2, y + ph - 10, 16, 12);
                    g.DrawEllipse(bootPen, x + 2, y + ph - 10, 16, 12);
                    g.FillEllipse(bootBrush, x + pw - 6, y + ph - 12, 18, 12);
                    g.DrawEllipse(bootPen, x + pw - 6, y + ph - 12, 18, 12);
                }
                else
                {
                    // picior din fata (stanga) avansat usor
                    g.FillEllipse(bootBrush, x - 6, y + ph - 12, 18, 12);
                    g.DrawEllipse(bootPen, x - 6, y + ph - 12, 18, 12);
                    g.FillEllipse(bootBrush, x + pw - 18, y + ph - 10, 16, 12);
                    g.DrawEllipse(bootPen, x + pw - 18, y + ph - 10, 16, 12);
                }
            }

            // Cap
            using var headBrush = new SolidBrush(headColor);
            g.FillEllipse(headBrush, x - 2, y, headR * 2, headR * 2);
            using var outlinePen = new Pen(Color.Black, 1.5f);
            g.DrawEllipse(outlinePen, x - 2, y, headR * 2, headR * 2);

            // Ochi
            g.FillEllipse(Brushes.White, x + 8, y + 8, 8, 8);
            g.FillEllipse(Brushes.Black, x + 10, y + 10, 4, 4);

            // Label
            string displayLabel = isYou ? "YOU" : label;
            using var font = new Font("Arial", 8, FontStyle.Bold);
            using var labelBrush = new SolidBrush(isYou ? Color.Yellow : Color.White);
            g.DrawString(displayLabel, font, labelBrush, x, y - 18);
        }

        private void DrawBall(Graphics g, float x, float y)
        {
            const float r = 20;
            // Minge alba cu dungi negre (simplu)
            g.FillEllipse(Brushes.White, x - r, y - r, r * 2, r * 2);
            using var pen = new Pen(Color.Black, 1.5f);
            g.DrawEllipse(pen, x - r, y - r, r * 2, r * 2);
            // Detalii minge
            g.DrawLine(pen, x, y - r, x, y + r);
            g.DrawLine(pen, x - r, y, x + r, y);
        }

        private void DrawHUD(Graphics g, GameState state)
        {
            // Fundal HUD
            using var hudBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            g.FillRectangle(hudBrush, 200, 5, 300, 45);

            // Scor
            using var scorFont = new Font("Arial", 22, FontStyle.Bold);
            string score = $"{state.Score1}  :  {state.Score2}";
            g.DrawString(score, scorFont, Brushes.White, 250, 8);

            // Timp
            using var timeFont = new Font("Arial", 10);
            string time = $"⏱ {state.TimeLeft}s";
            g.DrawString(time, timeFont, Brushes.Yellow, 320, 38);
        }

        public void DrawWaiting(Graphics g, int playerId)
        {
            g.Clear(Color.FromArgb(20, 20, 40));
            using var font = new Font("Arial", 16, FontStyle.Bold);
            string msg = playerId == 0
                ? "Se conecteaza la server..."
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
    }
}