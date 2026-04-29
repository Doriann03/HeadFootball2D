using System.Net;
using System.Net.Sockets;
using HeadFootball.Server;

Console.WriteLine("=== HeadFootball2D Server ===");

var db = new Database();
var lobby = new LobbyManager(db);

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();
Console.WriteLine("Server pornit pe portul 5000. Astept conexiuni...");

// Acceptam clienti la infinit
while (true)
{
    var tcpClient = listener.AcceptTcpClient();
    var handler = new ClientHandler(tcpClient, db, lobby);
    lobby.AddClient(handler);
    handler.Start();
    Console.WriteLine("Client nou conectat.");
}