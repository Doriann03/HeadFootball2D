namespace HeadFootball.Shared
{
    public class GameState
    {
        public float Player1X = 150;
        public float Player1Y = 300;
        public float Player2X = 550;
        public float Player2Y = 300;
        public float BallX = 350;
        public float BallY = 200;
        public int Score1 = 0;
        public int Score2 = 0;
        public int TimeLeft = 90;
        public bool GameStarted = false;
        public bool GameOver = false;
        public bool Player1Kicking = false;
        public bool Player2Kicking = false;

        // Power-up
        public bool PowerUpVisible = false;
        public float PowerUpX = 0;
        public float PowerUpY = 0;
        public int PowerUpType = 0; // 1=viteza, 2=saritura, 3=minge mare, 4=gol dublu
        public int Player1ActivePowerUp = 0;
        public int Player2ActivePowerUp = 0;
        public int Player1PowerUpTimer = 0;
        public int Player2PowerUpTimer = 0;
        public float BallScale = 1.0f; // pentru minge mai mare/mica

        public bool BallWasKicked = false;
    }
}