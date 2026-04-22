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

        // Returns: 0 = no goal, 1 = player1 scored, 2 = player2 scored
        public int Update(GameState state, PlayerInput input1, PlayerInput input2)
        {
            state.Player1Kicking = input1.Kick;
            state.Player2Kicking = input2.Kick;
            MovePlayer(ref state.Player1X, ref state.Player1Y, ref _vel1Y, input1, true);
            MovePlayer(ref state.Player2X, ref state.Player2Y, ref _vel2Y, input2, false);
            UpdateBall(state, input1, input2);

            // Check goal immediately and return scorer
            return CheckGoal(state);
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
            // Permitem jucatorilor sa treaca in jumatatea adversa, doar nu sa iasa din teren
            float minX = GoalWidth;
            float maxX = FieldWidth - GoalWidth - PlayerWidth;
            x = Math.Clamp(x, minX, maxX);
        }

        private void UpdateBall(GameState state, PlayerInput input1, PlayerInput input2)
        {
            // Stocam pozitia precedenta pentru detectii continue
            _prevBallX = state.BallX;
            _prevBallY = state.BallY;

            // Gravitatie minge
            _velBallY += BallGravity;
            _velBallX *= BallFriction;

            state.BallX += _velBallX;
            state.BallY += _velBallY;
            // Continuous check: did the ball's leading edge cross the goal line this frame?
            // This prevents tunneling and false non-goals.
            // Left goal
            float prevLeftLead = _prevBallX + BallRadius;
            float currLeftLead = state.BallX + BallRadius;
            if (prevLeftLead > GoalWidth && currLeftLead <= GoalWidth)
            {
                // compute interpolation factor t where leading edge hits the line
                float denom = prevLeftLead - currLeftLead;
                float t = denom != 0 ? (prevLeftLead - GoalWidth) / denom : 0f;
                float yAtCross = _prevBallY + (state.BallY - _prevBallY) * t;
                // check vertical position at crossing: under crossbar and above ground
                if (yAtCross - BallRadius >= GoalY && yAtCross + BallRadius <= GroundY)
                {
                    // place ball clearly inside goal so CheckGoal will detect it
                    state.BallX = GoalWidth - BallRadius - 1f;
                    state.BallY = Math.Clamp(yAtCross, GoalY + BallRadius, GroundY - BallRadius);
                    // skip further collision handling this frame
                    return;
                }
            }

            // Right goal
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
            // Verificam daca mingea este deja in interiorul porii (gol potential)
            // Folosim criteriu simplu si robust: mingea e considerata in poarta daca
            // - partea ei dinspre linia porții a trecut complet linia porții (BallX +/- BallRadius past line)
            // - mingea este sub bara transversala si deasupra terenului
            bool inLeftGoalArea = state.BallX + BallRadius <= GoalWidth
                                   && state.BallY - BallRadius >= GoalY
                                   && state.BallY + BallRadius <= GroundY;
            bool inRightGoalArea = state.BallX - BallRadius >= FieldWidth - GoalWidth
                                   && state.BallY - BallRadius >= GoalY
                                   && state.BallY + BallRadius <= GroundY;

            // Coliziune cu barele portilor (posturi si bara transversala)
            // Daca mingea e complet in interiorul portii, nu face ricoseu pe cadrul porții — va fi considerat gol
            if (!inLeftGoalArea && !inRightGoalArea)
            {
            // Vertical posts are non-solid now: do not reflect the ball on side posts so the ball
            // can enter the goal area and touch the net. We only keep the horizontal crossbar collision below.

                // Bara transversala (top crossbar) - doar daca mingea a penetrat bariera in acest frame
                // (prevenim "teleport" cand mingea este deja in interiorul porții sau atinge o zona de sus)
                float prevTop = _prevBallY - BallRadius;
                float currTop = state.BallY - BallRadius;

                // Stanga: detectam patrunderea de sus in jos
                if (prevTop <= GoalY && currTop > GoalY &&
                    state.BallX >= 0 && state.BallX <= GoalWidth)
                {
                    state.BallY = GoalY + BallRadius;
                    _velBallY *= -0.6f;
                }

                // Dreapta
                if (prevTop <= GoalY && currTop > GoalY &&
                    state.BallX >= FieldWidth - GoalWidth && state.BallX <= FieldWidth)
                {
                    state.BallY = GoalY + BallRadius;
                    _velBallY *= -0.6f;
                }
            }


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
            // Prima verifica: piciorul (solid) — piciorul este plasat in fata jucatorului
            bool isPlayer1 = kickDir > 0;
            float footOffsetX = PlayerWidth * 0.75f; // piciorul este aproape de marginea din fata
            float footCenterX = px + (isPlayer1 ? footOffsetX : PlayerWidth - footOffsetX);
            float footCenterY = py + PlayerHeight - 10; // aproape de sol
            float footRadius = 24f; // mai mare, solid

            float fdx = bx - footCenterX;
            float fdy = by - footCenterY;
            float fdist = (float)Math.Sqrt(fdx * fdx + fdy * fdy);

            if (fdist < BallRadius + footRadius)
            {
                float nx = fdx / fdist;
                float ny = fdy / fdist;

                // Coliziune cu piciorul: daca este sut, impuls mai mare si arc in sus
                if (kick)
                {
                    _velBallX = nx * 14f + kickDir * 5f;
                    _velBallY = ny * 8f - 10f;
                }
                else
                {
                    // atingere normala - mica schimbare de directie
                    _velBallX = nx * 6f + kickDir * 1.5f;
                    _velBallY = ny * 4f - 2f;
                }

                bx = footCenterX + nx * (BallRadius + footRadius + 1);
                by = footCenterY + ny * (BallRadius + footRadius + 1);

                return; // deja procesata coliziunea cu piciorul
            }

            // Daca nu a lovit piciorul, verificam coliziunea generala cu corpul/capul jucatorului (mai sus)
            float centerX = px + PlayerWidth / 2;
            float centerY = py + PlayerHeight / 4; // capul e sus

            float dx = bx - centerX;
            float dy = by - centerY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < BallRadius + PlayerWidth / 2)
            {
                float nx = dx / dist;
                float ny = dy / dist;

                // When kicking, give the ball a stronger forward push and a noticeable upward arc
                _velBallX = nx * (kick ? 12f : 6f) + kickDir * (kick ? 4f : 0);
                _velBallY = ny * 6f + (kick ? -8f : 0);

                bx = centerX + nx * (BallRadius + PlayerWidth / 2 + 1);
                by = centerY + ny * (BallRadius + PlayerWidth / 2 + 1);
            }
        }

        public int CheckGoal(GameState state)
        {
            // Gol doar daca mingea a intrat efectiv in poarta (toata mingea in interiorul zonei portii)
            // Folosim acelasi criteriu ca in UpdateBall: partea mingii orientata spre linia porții
            // trebuie sa treaca complet linia porții, si mingea trebuie sa fie sub bara transversala
            bool inLeftGoalArea = state.BallX + BallRadius <= GoalWidth
                                   && state.BallY - BallRadius >= GoalY
                                   && state.BallY + BallRadius <= GroundY;
            bool inRightGoalArea = state.BallX - BallRadius >= FieldWidth - GoalWidth
                                   && state.BallY - BallRadius >= GoalY
                                   && state.BallY + BallRadius <= GroundY;

            if (inLeftGoalArea)
            {
                ResetBall(state);
                return 2;
            }

            if (inRightGoalArea)
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