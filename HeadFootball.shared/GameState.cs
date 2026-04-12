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
    }
}