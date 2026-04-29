using HeadFootball.Shared;
using Microsoft.Data.Sqlite;

namespace HeadFootball.Server
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string dbPath = "headfootball.db")
        {
            _connectionString = $"Data Source={dbPath}";
            Initialize();
        }

        // Creeaza tabelele daca nu exista
        private void Initialize()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT UNIQUE NOT NULL,
                    password TEXT NOT NULL,
                    rating INTEGER DEFAULT 1000,
                    created_at TEXT DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS matches (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    player1_id INTEGER NOT NULL,
                    player2_id INTEGER NOT NULL,
                    score1 INTEGER NOT NULL,
                    score2 INTEGER NOT NULL,
                    played_at TEXT DEFAULT (datetime('now')),
                    FOREIGN KEY (player1_id) REFERENCES users(id),
                    FOREIGN KEY (player2_id) REFERENCES users(id)
                );

                CREATE TABLE IF NOT EXISTS stats (
                    user_id INTEGER PRIMARY KEY,
                    wins INTEGER DEFAULT 0,
                    losses INTEGER DEFAULT 0,
                    draws INTEGER DEFAULT 0,
                    goals_scored INTEGER DEFAULT 0,
                    goals_conceded INTEGER DEFAULT 0,
                    FOREIGN KEY (user_id) REFERENCES users(id)
                );
            ";
            cmd.ExecuteNonQuery();
            Console.WriteLine("Baza de date initializata.");
        }

        // Inregistreaza un user nou
        // Returneaza: (true, "OK", id) sau (false, "mesaj eroare", -1)
        public (bool success, string message, int userId) Register(string username, string password)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                // Verificam daca username-ul exista deja
                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = $u";
                checkCmd.Parameters.AddWithValue("$u", username);
                long count = (long)checkCmd.ExecuteScalar()!;
                if (count > 0)
                    return (false, "Username-ul este deja folosit.", -1);

                // Inseram userul nou
                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO users (username, password) VALUES ($u, $p);
                    SELECT last_insert_rowid();
                ";
                insertCmd.Parameters.AddWithValue("$u", username);
                insertCmd.Parameters.AddWithValue("$p", password); // in productie: hash parola!
                long newId = (long)insertCmd.ExecuteScalar()!;

                // Cream si intrarea in stats
                var statsCmd = conn.CreateCommand();
                statsCmd.CommandText = "INSERT INTO stats (user_id) VALUES ($id)";
                statsCmd.Parameters.AddWithValue("$id", newId);
                statsCmd.ExecuteNonQuery();

                return (true, "Cont creat cu succes!", (int)newId);
            }
            catch (Exception ex)
            {
                return (false, $"Eroare server: {ex.Message}", -1);
            }
        }

        // Login
        // Returneaza: (true, "OK", id) sau (false, "mesaj eroare", -1)
        public (bool success, string message, int userId) Login(string username, string password)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id FROM users WHERE username = $u AND password = $p";
                cmd.Parameters.AddWithValue("$u", username);
                cmd.Parameters.AddWithValue("$p", password);

                var result = cmd.ExecuteScalar();
                if (result == null)
                    return (false, "Username sau parola gresite.", -1);

                return (true, "Login reusit!", (int)(long)result);
            }
            catch (Exception ex)
            {
                return (false, $"Eroare server: {ex.Message}", -1);
            }
        }

        // Salveaza rezultatul unui meci si actualizeaza statisticile
        public void SaveMatch(int player1Id, int player2Id, int score1, int score2)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // Salvam meciul
            var matchCmd = conn.CreateCommand();
            matchCmd.CommandText = @"
                INSERT INTO matches (player1_id, player2_id, score1, score2)
                VALUES ($p1, $p2, $s1, $s2)
            ";
            matchCmd.Parameters.AddWithValue("$p1", player1Id);
            matchCmd.Parameters.AddWithValue("$p2", player2Id);
            matchCmd.Parameters.AddWithValue("$s1", score1);
            matchCmd.Parameters.AddWithValue("$s2", score2);
            matchCmd.ExecuteNonQuery();

            // Actualizam stats player1
            string result1 = score1 > score2 ? "win" : score1 < score2 ? "loss" : "draw";
            string result2 = score2 > score1 ? "win" : score2 < score1 ? "loss" : "draw";

            UpdateStats(conn, player1Id, result1, score1, score2);
            UpdateStats(conn, player2Id, result2, score2, score1);

            // Actualizam rating ELO simplu
            UpdateRating(conn, player1Id, player2Id, score1, score2);
        }

        private void UpdateStats(SqliteConnection conn, int userId,
                                  string result, int goalsFor, int goalsAgainst)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = result switch
            {
                "win" => "UPDATE stats SET wins = wins + 1, goals_scored = goals_scored + $gf, goals_conceded = goals_conceded + $ga WHERE user_id = $id",
                "loss" => "UPDATE stats SET losses = losses + 1, goals_scored = goals_scored + $gf, goals_conceded = goals_conceded + $ga WHERE user_id = $id",
                _ => "UPDATE stats SET draws = draws + 1, goals_scored = goals_scored + $gf, goals_conceded = goals_conceded + $ga WHERE user_id = $id"
            };
            cmd.Parameters.AddWithValue("$gf", goalsFor);
            cmd.Parameters.AddWithValue("$ga", goalsAgainst);
            cmd.Parameters.AddWithValue("$id", userId);
            cmd.ExecuteNonQuery();
        }

        private void UpdateRating(SqliteConnection conn, int p1Id, int p2Id,
                                   int score1, int score2)
        {
            // Luam ratingurile actuale
            int r1 = GetRating(conn, p1Id);
            int r2 = GetRating(conn, p2Id);

            // ELO simplu
            double expected1 = 1.0 / (1.0 + Math.Pow(10, (r2 - r1) / 400.0));
            double actual1 = score1 > score2 ? 1.0 : score1 < score2 ? 0.0 : 0.5;
            double actual2 = 1.0 - actual1;
            double expected2 = 1.0 - expected1;

            int K = 32;
            int newR1 = r1 + (int)(K * (actual1 - expected1));
            int newR2 = r2 + (int)(K * (actual2 - expected2));

            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET rating = $r WHERE id = $id";
            cmd.Parameters.AddWithValue("$r", newR1);
            cmd.Parameters.AddWithValue("$id", p1Id);
            cmd.ExecuteNonQuery();

            cmd.Parameters["$r"].Value = newR2;
            cmd.Parameters["$id"].Value = p2Id;
            cmd.ExecuteNonQuery();
        }

        private int GetRating(SqliteConnection conn, int userId)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT rating FROM users WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", userId);
            return (int)(long)cmd.ExecuteScalar()!;
        }

        // Returneaza top 10 jucatori dupa rating
        public List<StatsPayload> GetLeaderboard()
        {
            var result = new List<StatsPayload>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT u.username, u.rating, s.wins, s.losses, s.draws,
                       s.goals_scored, s.goals_conceded
                FROM users u
                JOIN stats s ON u.id = s.user_id
                ORDER BY u.rating DESC
                LIMIT 10
            ";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new StatsPayload
                {
                    Username = reader.GetString(0),
                    Rating = reader.GetInt32(1),
                    Wins = reader.GetInt32(2),
                    Losses = reader.GetInt32(3),
                    Draws = reader.GetInt32(4),
                    GoalsScored = reader.GetInt32(5),
                    GoalsConceded = reader.GetInt32(6)
                });
            }
            return result;
        }

        // Returneaza statisticile unui jucator
        public StatsPayload? GetStats(string username)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT u.username, u.rating, s.wins, s.losses, s.draws,
                       s.goals_scored, s.goals_conceded
                FROM users u
                JOIN stats s ON u.id = s.user_id
                WHERE u.username = $u
            ";
            cmd.Parameters.AddWithValue("$u", username);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new StatsPayload
                {
                    Username = reader.GetString(0),
                    Rating = reader.GetInt32(1),
                    Wins = reader.GetInt32(2),
                    Losses = reader.GetInt32(3),
                    Draws = reader.GetInt32(4),
                    GoalsScored = reader.GetInt32(5),
                    GoalsConceded = reader.GetInt32(6)
                };
            }
            return null;
        }
    }
}