using HeadFootball.Shared;

namespace Headfootball.Client
{
    public class GameRenderer
    {
        private const float FieldW = 700;
        private const float FieldH = 400;
        private const float GroundY = 330;
        private const float GoalH = 120;
        private const float GoalW = 40;

        public void Draw(Graphics g, GameState state, int playerId)
        {
            g.Clear(Color.FromArgb(34, 139, 34));

            using var whitePen = new Pen(Color.White, 2);
            g.DrawLine(whitePen, FieldW / 2, 0, FieldW / 2, GroundY);
            g.DrawEllipse(whitePen, FieldW / 2 - 50, GroundY / 2 - 50, 100, 100);

            using var groundBrush = new SolidBrush(Color.SaddleBrown);
            g.FillRectangle(groundBrush, 0, GroundY, FieldW, FieldH - GroundY);

            using var goalBrush = new SolidBrush(Color.White);
            using var goalPen = new Pen(Color.Gray, 3);
            g.FillRectangle(goalBrush, 0, GroundY - GoalH, GoalW, GoalH);
            g.DrawRectangle(goalPen, 0, GroundY - GoalH, GoalW, GoalH);
            g.FillRectangle(goalBrush, FieldW - GoalW, GroundY - GoalH, GoalW, GoalH);
            g.DrawRectangle(goalPen, FieldW - GoalW, GroundY - GoalH, GoalW, GoalH);

            // Power-up pe teren
            if (state.PowerUpVisible)
                DrawPowerUp(g, state.PowerUpX, state.PowerUpY, state.PowerUpType);

            DrawPlayer(g, state.Player1X, state.Player1Y, Color.DodgerBlue,
                       Color.DarkBlue, "P1", playerId == 1,
                       state.Player1Kicking, true);

            DrawPlayer(g, state.Player2X, state.Player2Y, Color.Tomato,
                       Color.DarkRed, "P2", playerId == 2,
                       state.Player2Kicking, false);

            DrawBall(g, state.BallX, state.BallY, state.BallScale);
            DrawPowerUpIndicators(g, state);
            DrawHUD(g, state);
        }

        private void DrawPlayer(Graphics g, float x, float y,
                                  Color bodyColor, Color headColor,
                                  string label, bool isYou,
                                  bool isKicking, bool facingRight)
        {
            const float pw = 40, ph = 60;
            const float headR = 22;

            using var bodyBrush = new SolidBrush(bodyColor);
            g.FillRectangle(bodyBrush, x, y + ph / 2, pw, ph / 2);

            using var bootBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            using var bootPen = new Pen(Color.Black, 1.5f);

            if (isKicking)
            {
                float kickX = facingRight ? x + pw : x - 18;
                float kickY = y + ph / 2 + 5;
                g.FillEllipse(bootBrush, kickX, kickY, 22, 12);
                g.DrawEllipse(bootPen, kickX, kickY, 22, 12);
            }
            else
            {
                g.FillEllipse(bootBrush, x + 2, y + ph - 10, 16, 12);
                g.DrawEllipse(bootPen, x + 2, y + ph - 10, 16, 12);
                g.FillEllipse(bootBrush, x + pw - 18, y + ph - 10, 16, 12);
                g.DrawEllipse(bootPen, x + pw - 18, y + ph - 10, 16, 12);
            }

            using var headBrush = new SolidBrush(headColor);
            g.FillEllipse(headBrush, x - 2, y, headR * 2, headR * 2);
            using var outlinePen = new Pen(Color.Black, 1.5f);
            g.DrawEllipse(outlinePen, x - 2, y, headR * 2, headR * 2);

            g.FillEllipse(Brushes.White, x + 8, y + 8, 8, 8);
            g.FillEllipse(Brushes.Black, x + 10, y + 10, 4, 4);

            string displayLabel = isYou ? "YOU" : label;
            using var font = new Font("Arial", 8, FontStyle.Bold);
            using var labelBrush = new SolidBrush(isYou ? Color.Yellow : Color.White);
            g.DrawString(displayLabel, font, labelBrush, x, y - 18);
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
            using var hudBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            g.FillRectangle(hudBrush, 200, 5, 300, 45);

            using var scorFont = new Font("Arial", 22, FontStyle.Bold);
            string score = $"{state.Score1}  :  {state.Score2}";
            g.DrawString(score, scorFont, Brushes.White, 250, 8);

            using var timeFont = new Font("Arial", 10);
            string time = $"⏱ {state.TimeLeft}s";
            g.DrawString(time, timeFont, Brushes.Yellow, 320, 38);
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
    }
}