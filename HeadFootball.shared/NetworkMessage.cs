using System.Collections.Generic;

namespace HeadFootball.Shared
{
    public enum MessageType
    {
        PlayerInput, GameState, PlayerAssigned, GameStart, GameOver,
        Login, Register, LoginOk, LoginFail,
        RoomList, CreateRoom, JoinRoom, JoinAsSpectator, RoomJoined, LeaveRoom,
        ChatMessage,
        StatsRequest, StatsResponse,
        LeaderboardRequest, LeaderboardResponse
    }

    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public string? Payload { get; set; }
    }

    public class LoginPayload
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterPayload
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginResultPayload
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int PlayerId { get; set; }
    }

    public class ChatPayload
    {
        public string Sender { get; set; } = "";
        public string Message { get; set; } = "";
        public string Room { get; set; } = "lobby";
    }

    public class RoomInfo
    {
        public string RoomId { get; set; } = "";
        public string HostName { get; set; } = "";
        public int PlayerCount { get; set; }
        public int SpectatorCount { get; set; }
        public bool InProgress { get; set; }
    }

    public class RoomListPayload
    {
        public List<RoomInfo> Rooms { get; set; } = new();
    }

    public class JoinRoomPayload
    {
        public string RoomId { get; set; } = "";
        public bool AsSpectator { get; set; }
    }

    public class StatsPayload
    {
        public string Username { get; set; } = "";
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int GoalsScored { get; set; }
        public int GoalsConceded { get; set; }
        public int Rating { get; set; }
    }
}