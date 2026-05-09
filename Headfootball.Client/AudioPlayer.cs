using System;
using System.Collections.Generic;
using System.Windows.Media; // Această bibliotecă modernă este acum disponibilă

namespace Headfootball.Client
{
    public static class AudioPlayer
    {
        // Păstrăm o listă cu playerele noastre în memorie
        private static Dictionary<string, MediaPlayer> _players = new();

        public static void Load(string filePath, string alias)
        {
            var player = new MediaPlayer();
            player.Open(new Uri(filePath, UriKind.Absolute));
            _players[alias] = player;
        }

        public static void Play(string alias, bool loop = false)
        {
            if (_players.TryGetValue(alias, out var player))
            {
                // Resetăm sunetul la secunda 0
                player.Position = TimeSpan.Zero;

                if (loop)
                {
                    // Dacă vrem să se repete la infinit (pentru ambient)
                    player.MediaEnded -= Player_MediaEnded; // Prevenim dublarea
                    player.MediaEnded += Player_MediaEnded;
                }

                player.Play();
            }
        }

        private static void Player_MediaEnded(object? sender, EventArgs e)
        {
            if (sender is MediaPlayer player)
            {
                player.Position = TimeSpan.Zero;
                player.Play(); // Repornește automat
            }
        }

        public static void Stop(string alias)
        {
            if (_players.TryGetValue(alias, out var player))
            {
                player.Stop();
            }
        }
    }
}