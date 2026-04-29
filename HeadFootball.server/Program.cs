using System.Net;
using System.Net.Sockets;
using HeadFootball.Server;

Console.WriteLine("=== HeadFootball2D Server ===");

// Initializare baza de date
var db = new Database();

Console.WriteLine("Astept 2 jucatori...");

var listener = new TcpListener(IPAddress.Any, 5000);
listener.Start();

TcpClient client1 = listener.AcceptTcpClient();
Console.WriteLine("Jucatorul 1 conectat!");

TcpClient client2 = listener.AcceptTcpClient();
Console.WriteLine("Jucatorul 2 conectat!");

Console.WriteLine("Ambii jucatori conectati. Pornesc jocul...");

var room = new GameRoom(client1, client2, db);
room.Start();

Console.WriteLine("Joc terminat.");