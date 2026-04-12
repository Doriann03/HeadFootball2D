using System;
using System.Collections.Generic;
using System.Text;

namespace HeadFootball.Shared
{
    public enum MessageType
    {
        PlayerInput,
        GameState,
        PlayerAssigned,
        GameStart,
        GameOver
    }

    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public string? Payload { get; set; }
    }
}