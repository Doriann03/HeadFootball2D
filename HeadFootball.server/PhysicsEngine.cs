using HeadFootball.Shared;

namespace HeadFootball.Server
{
    public class PhysicsEngine
    {
        // Dimensiuni teren
        public const float FieldWidth = 700;
        public const float FieldHeight = 400;
        public const float GroundY = 330;

        // Porti
        public const float GoalWidth = 20;
        public const float GoalHeight = 80;
        public const float GoalY = FieldHeight - GoalHeight;

        // Jucatori
        public const float PlayerWidth = 40;
        public const float PlayerHeight = 60;
        public const float PlayerSpeed = 4f;
        public const float JumpForce = -12f;
        public const float Gravity = 0.5f;

        // Minge
        public const float BallRadius = 20;
        public const float BallGravity = 0.4f;
        public const float BallFriction = 0.98f;

        // Viteze jucatori
        private float _vel1Y = 0, _vel2Y = 0;
        private float _velBallX = 2, _velBallY = -3;

        public void Update(GameState state, PlayerInput input1, PlayerInput input2)
        {
            MovePlayer(ref state.Player1X, ref state.Player1Y, ref _vel1Y, input1, true);
            MovePlayer(ref state.Player2X, ref state.Player2Y, ref _vel2Y, input2, false);
            UpdateBall(state, input1, input2);
        }

        private void MovePlayer(ref float x, ref float y, ref float velY,
                                 PlayerInput input, bool isPlayer1)
        {
            // Miscare orizontala
            if (input.Left) x -= PlayerSpeed;
            if (input.Right) x += PlayerSpeed;

            // Saritura
            if (input.Jump && y >= GroundY - PlayerHeight)
                velY = JumpForce;

            // Gravitatie
            velY += Gravity;
            y += velY;

            // Sol
            if (y >= GroundY - PlayerHeight)
            {
                y = GroundY - PlayerHeight;
                velY = 0;
            }

            // Margini teren
            float minX = isPlayer1 ? GoalWidth : FieldWidth / 2 + 5;
            float maxX = isPlayer1 ? FieldWidth / 2 - 5 : FieldWidth - GoalWidth - PlayerWidth;
            x = Math.Clamp(x, minX, maxX);
        }

        private void UpdateBall(GameState state, PlayerInput input1, PlayerInput input2)
        {
            // Gravitatie minge
            _velBallY += BallGravity;
            _velBallX *= BallFriction;

            state.BallX += _velBallX;
            state.BallY += _velBallY;

            // Sol
            if (state.BallY >= GroundY - BallRadius)
            {
                state.BallY = GroundY - BallRadius;
                _velBallY *= -0.6f;
                _velBallX *= 0.85f;
            }

            // Margini laterale
            if (state.BallX <= BallRadius) { state.BallX = BallRadius; _velBallX *= -0.8f; }
            if (state.BallX >= FieldWidth - BallRadius) { state.BallX = FieldWidth - BallRadius; _velBallX *= -0.8f; }

            // Tavan
            if (state.BallY <= BallRadius) { state.BallY = BallRadius; _velBallY *= -0.6f; }

            // Coliziune cu jucatorul 1
            CheckPlayerBallCollision(state.Player1X, state.Player1Y, ref state.BallX,
                ref state.BallY, input1.Kick, 1);

            // Coliziune cu jucatorul 2
            CheckPlayerBallCollision(state.Player2X, state.Player2Y, ref state.BallX,
                ref state.BallY, input2.Kick, -1);
        }

        private void CheckPlayerBallCollision(float px, float py, ref float bx, ref float by,
                                               bool kick, float kickDir)
        {
            float centerX = px + PlayerWidth / 2;
            float centerY = py + PlayerHeight / 4; // capul e sus

            float dx = bx - centerX;
            float dy = by - centerY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < BallRadius + PlayerWidth / 2)
            {
                float nx = dx / dist;
                float ny = dy / dist;

                _velBallX = nx * (kick ? 10f : 6f) + kickDir * (kick ? 3f : 0);
                _velBallY = ny * 6f + (kick ? -4f : 0);

                bx = centerX + nx * (BallRadius + PlayerWidth / 2 + 1);
                by = centerY + ny * (BallRadius + PlayerWidth / 2 + 1);
            }
        }

        public int CheckGoal(GameState state)
        {
            // Gol in poarta stanga (Player2 marcheaza)
            if (state.BallX - BallRadius <= GoalWidth &&
                state.BallY >= GoalY)
            {
                ResetBall(state);
                return 2;
            }

            // Gol in poarta dreapta (Player1 marcheaza)
            if (state.BallX + BallRadius >= FieldWidth - GoalWidth &&
                state.BallY >= GoalY)
            {
                ResetBall(state);
                return 1;
            }

            return 0;
        }

        private void ResetBall(GameState state)
        {
            state.BallX = FieldWidth / 2;
            state.BallY = FieldHeight / 2;
            _velBallX = 0;
            _velBallY = 0;
        }
    }
}