using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Generic;

namespace HeadFootball.Shared
{
    public enum MessageType
    {
        // Login/Register
        Login,
        Register,
        LoginOk,
        LoginFail,

        // Lobby and rooms
        RoomList,
        CreateRoom,
        JoinRoom,
        JoinAsSpectator,
        RoomJoined,
        LeaveRoom,

        // Chat
        ChatMessage,

        // Statistics
        StatsRequest,
        StatsResponse,

        // Game-related
        PlayerInput,
        GameState,
        PlayerAssigned,
        GameStart,
        GameOver
    }

    /// <summary>
    /// General network message wrapper containing the message type and a JSON/string payload.
    /// </summary>
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public string? Payload { get; set; }
    }

    /// <summary>
    /// Payload sent to request a login.
    /// </summary>
    public class LoginPayload
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Payload sent to register a new account.
    /// </summary>
    public class RegisterPayload
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Result of a login attempt.
    /// </summary>
    public class LoginResultPayload
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int PlayerId { get; set; }
    }

    /// <summary>
    /// Chat message payload. Room can be "lobby" or a specific room id.
    /// </summary>
    public class ChatPayload
    {
        public string Sender { get; set; }
        public string Message { get; set; }
        public string Room { get; set; }
    }

    /// <summary>
    /// Information about a single room.
    /// </summary>
    public class RoomInfo
    {
        public string RoomId { get; set; }
        public string HostName { get; set; }
        public int PlayerCount { get; set; }
        public int SpectatorCount { get; set; }
        public bool InProgress { get; set; }
    }

    /// <summary>
    /// Payload that contains a list of available rooms.
    /// </summary>
    public class RoomListPayload
    {
        public List<RoomInfo> Rooms { get; set; }
    }

    /// <summary>
    /// Payload to request joining a room.
    /// </summary>
    public class JoinRoomPayload
    {
        public string RoomId { get; set; }
        public bool AsSpectator { get; set; }
    }

    /// <summary>
    /// Payload containing player statistics.
    /// </summary>
    public class StatsPayload
    {
        public string Username { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int GoalsScored { get; set; }
        public int GoalsConceded { get; set; }
        public int Rating { get; set; }
    }
}
