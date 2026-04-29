namespace Headfootball.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var network = new NetworkClient();

            // Conectare la server in background
            Task.Run(() =>
            {
                try { network.Connect("127.0.0.1", 5000); }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nu ma pot conecta la server!\n{ex.Message}",
                        "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            Application.Run(new LoginForm(network));
        }
    }
}