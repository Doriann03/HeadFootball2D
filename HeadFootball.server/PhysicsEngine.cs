using HeadFootball.Shared;

namespace HeadFootball.Server
{
    public class PhysicsEngine
    {
        // Dimensiuni teren
        public const float FieldWidth = 700;
        public const float FieldHeight = 400;
        public const float GroundY = 330;

        // Porti (marite)
        public const float GoalWidth = 40;
        public const float GoalHeight = 120;
        // Grosimea ramei porții (posturi + bara transversala)
        public const float GoalFrame = 6f;
        // Aliniem GoalY cu GroundY - GoalHeight pentru consistenta cu renderer
        public const float GoalY = GroundY - GoalHeight;

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
        // Previous ball position for continuous collision/goal detection
        private float _prevBallX = FieldWidth / 2;
        private float _prevBallY = FieldHeight / 2;

        private Random _rng = new();

        // Schimbat din 'int' in 'void'. Lăsăm doar GameRoom să apeleze CheckGoal!
        public void Update(GameState state, PlayerInput input1, PlayerInput input2)
        {
            state.Player1Kicking = input1.Kick;
            state.Player2Kicking = input2.Kick;

            // Calculam viteza si saritura luand in calcul power-up-urile!
            float speed1 = state.Player1ActivePowerUp == 1 ? PlayerSpeed * 1.6f : PlayerSpeed;
            float speed2 = state.Player2ActivePowerUp == 1 ? PlayerSpeed * 1.6f : PlayerSpeed;

            float jump1 = state.Player1ActivePowerUp == 2 ? JumpForce * 1.4f : JumpForce;
            float jump2 = state.Player2ActivePowerUp == 2 ? JumpForce * 1.4f : JumpForce;

            MovePlayer(ref state.Player1X, ref state.Player1Y, ref _vel1Y, input1, speed1, jump1);
            MovePlayer(ref state.Player2X, ref state.Player2Y, ref _vel2Y, input2, speed2, jump2);

            UpdateBall(state, input1, input2);

            // Gestionare Power-Up-uri
            UpdatePowerUpTimers(state);
            CheckPowerUpCollision(state);
        }

        private void MovePlayer(ref float x, ref float y, ref float velY,
                                 PlayerInput input, float currentSpeed, float currentJumpForce)
        {
            // Miscare orizontala
            if (input.Left) x -= currentSpeed;
            if (input.Right) x += currentSpeed;

            // Saritura
            if (input.Jump && y >= GroundY - PlayerHeight)
                velY = currentJumpForce;

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
            float minX = GoalWidth;
            float maxX = FieldWidth - GoalWidth - PlayerWidth;
            x = Math.Clamp(x, minX, maxX);
        }

        private void UpdateBall(GameState state, PlayerInput input1, PlayerInput input2)
        {
            _prevBallX = state.BallX;
            _prevBallY = state.BallY;

            _velBallY += BallGravity;
            _velBallX *= BallFriction;

            state.BallX += _velBallX;
            state.BallY += _velBallY;

            float prevLeftLead = _prevBallX + BallRadius;
            float currLeftLead = state.BallX + BallRadius;
            if (prevLeftLead > GoalWidth && currLeftLead <= GoalWidth)
            {
                float denom = prevLeftLead - currLeftLead;
                float t = denom != 0 ? (prevLeftLead - GoalWidth) / denom : 0f;
                float yAtCross = _prevBallY + (state.BallY - _prevBallY) * t;
                if (yAtCross - BallRadius >= GoalY && yAtCross + BallRadius <= GroundY)
                {
                    state.BallX = GoalWidth - BallRadius - 1f;
                    state.BallY = Math.Clamp(yAtCross, GoalY + BallRadius, GroundY - BallRadius);
                    return;
                }
            }

            float prevRightLead = _prevBallX - BallRadius;
            float currRightLead = state.BallX - BallRadius;
            if (prevRightLead < FieldWidth - GoalWidth && currRightLead >= FieldWidth - GoalWidth)
            {
                float denom = currRightLead - prevRightLead;
                float t = denom != 0 ? (FieldWidth - GoalWidth - prevRightLead) / denom : 0f;
                float yAtCross = _prevBallY + (state.BallY - _prevBallY) * t;
                if (yAtCross - BallRadius >= GoalY && yAtCross + BallRadius <= GroundY)
                {
                    state.BallX = FieldWidth - GoalWidth + BallRadius + 1f;
                    state.BallY = Math.Clamp(yAtCross, GoalY + BallRadius, GroundY - BallRadius);
                    return;
                }
            }

            bool inLeftGoalArea = state.BallX + BallRadius <= GoalWidth
                                   && state.BallY - BallRadius >= GoalY
                                   && state.BallY + BallRadius <= GroundY;
            bool inRightGoalArea = state.BallX - BallRadius >= FieldWidth - GoalWidth
                                   && state.BallY - BallRadius >= GoalY
                                   && state.BallY + BallRadius <= GroundY;

            if (!inLeftGoalArea && !inRightGoalArea)
            {
                float prevTop = _prevBallY - BallRadius;
                float currTop = state.BallY - BallRadius;

                if (prevTop <= GoalY && currTop > GoalY &&
                    state.BallX >= 0 && state.BallX <= GoalWidth)
                {
                    state.BallY = GoalY + BallRadius;
                    _velBallY *= -0.6f;
                }

                if (prevTop <= GoalY && currTop > GoalY &&
                    state.BallX >= FieldWidth - GoalWidth && state.BallX <= FieldWidth)
                {
                    state.BallY = GoalY + BallRadius;
                    _velBallY *= -0.6f;
                }
            }

            if (state.BallY >= GroundY - BallRadius)
            {
                state.BallY = GroundY - BallRadius;
                _velBallY *= -0.6f;
                _velBallX *= 0.85f;
            }

            if (state.BallX <= BallRadius) { state.BallX = BallRadius; _velBallX *= -0.8f; }
            if (state.BallX >= FieldWidth - BallRadius) { state.BallX = FieldWidth - BallRadius; _velBallX *= -0.8f; }

            if (state.BallY <= BallRadius) { state.BallY = BallRadius; _velBallY *= -0.6f; }

            // Coliziune cu jucatorii
            CheckPlayerBallCollision(state.Player1X, state.Player1Y, ref state.BallX,
                ref state.BallY, input1.Kick, 1, BallRadius * state.BallScale);

            CheckPlayerBallCollision(state.Player2X, state.Player2Y, ref state.BallX,
                ref state.BallY, input2.Kick, -1, BallRadius * state.BallScale);
        }

        private void CheckPlayerBallCollision(float px, float py, ref float bx, ref float by,
                                               bool kick, float kickDir, float radius)
        {
            float centerX = px + PlayerWidth / 2;
            float centerY = py + PlayerHeight / 4;
            float dx = bx - centerX;
            float dy = by - centerY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            float bodyRadius = radius + PlayerWidth / 2;
            float interactionRadius = bodyRadius + (kick ? 25f : 0f);

            if (dist < interactionRadius)
            {
                float nx = dx / dist;
                float ny = dy / dist;

                if (!kick)
                {
                    _velBallX = nx * 6f;
                    _velBallY = ny * 6f;

                    bx = centerX + nx * (bodyRadius + 1);
                    by = centerY + ny * (bodyRadius + 1);
                }
                else
                {
                    _velBallX = (nx * 8f) + (kickDir * 8f);
                    _velBallY = (ny * 4f) - 8f;

                    if (dist < bodyRadius)
                    {
                        bx = centerX + nx * (bodyRadius + 1);
                        by = centerY + ny * (bodyRadius + 1);
                    }
                }
            }
        }

        public int CheckGoal(GameState state)
        {
            float radius = BallRadius * state.BallScale;
            bool isInGoalHeight = state.BallY >= (GroundY - GoalHeight);

            if (isInGoalHeight && (state.BallX - radius <= GoalWidth))
            {
                int goals = state.Player2ActivePowerUp == 4 ? 2 : 1;
                ResetBall(state);
                SpawnPowerUp(state);
                return 20 + goals;
            }

            if (isInGoalHeight && (state.BallX + radius >= FieldWidth - GoalWidth))
            {
                int goals = state.Player1ActivePowerUp == 4 ? 2 : 1;
                ResetBall(state);
                SpawnPowerUp(state);
                return 10 + goals;
            }

            return 0;
        }

        private void CheckPowerUpCollision(GameState state)
        {
            if (!state.PowerUpVisible) return;
            float pwSize = 25;

            // Player 1
            if (state.Player1X < state.PowerUpX + pwSize && state.Player1X + PlayerWidth > state.PowerUpX &&
                state.Player1Y < state.PowerUpY + pwSize && state.Player1Y + PlayerHeight > state.PowerUpY)
            {
                ApplyPowerUp(state, 1, state.PowerUpType);
                state.PowerUpVisible = false;
            }
            // Player 2
            else if (state.Player2X < state.PowerUpX + pwSize && state.Player2X + PlayerWidth > state.PowerUpX &&
                     state.Player2Y < state.PowerUpY + pwSize && state.Player2Y + PlayerHeight > state.PowerUpY)
            {
                ApplyPowerUp(state, 2, state.PowerUpType);
                state.PowerUpVisible = false;
            }
        }

        private void ApplyPowerUp(GameState state, int player, int type)
        {
            // type 1 = viteza, 2 = saritura, 3 = minge marita, 4 = gol dublu
            int duration = 300; // ~5 secunde la 60fps pentru Viteza si Saritura

            if (type == 3) // Minge mare/mica schimba mingea instant
            {
                state.BallScale = _rng.Next(0, 2) == 0 ? 1.8f : 0.5f;
                return;
            }

            // Pentru Gol Dublu, punem timer infinit (se va reseta automat cand cineva da gol datorita metodei ResetBall)
            if (type == 4)
            {
                duration = 999999;
            }

            if (player == 1)
            {
                state.Player1ActivePowerUp = type;
                state.Player1PowerUpTimer = duration;
            }
            else
            {
                state.Player2ActivePowerUp = type;
                state.Player2PowerUpTimer = duration;
            }
        }

        private void UpdatePowerUpTimers(GameState state)
        {
            if (state.Player1PowerUpTimer > 0)
            {
                state.Player1PowerUpTimer--;
                if (state.Player1PowerUpTimer <= 0) state.Player1ActivePowerUp = 0;
            }
            if (state.Player2PowerUpTimer > 0)
            {
                state.Player2PowerUpTimer--;
                if (state.Player2PowerUpTimer <= 0) state.Player2ActivePowerUp = 0;
            }
        }

        private void SpawnPowerUp(GameState state)
        {
            state.PowerUpVisible = true;
            state.PowerUpX = _rng.Next(100, 600);
            state.PowerUpY = _rng.Next(150, 280);
            state.PowerUpType = _rng.Next(1, 5);
        }

        private void ResetBall(GameState state)
        {
            state.BallX = FieldWidth / 2;
            state.BallY = FieldHeight / 2;
            _velBallX = 0;
            _velBallY = 0;

            // Resetam și efectele cand se da un gol
            state.BallScale = 1.0f;
            state.Player1ActivePowerUp = 0;
            state.Player2ActivePowerUp = 0;
            state.Player1PowerUpTimer = 0;
            state.Player2PowerUpTimer = 0;
        }
    }
}