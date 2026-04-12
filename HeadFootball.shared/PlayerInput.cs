using System;
using System.Collections.Generic;
using System.Text;

namespace HeadFootball.Shared
{
    public class PlayerInput
    {
        public int PlayerId { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Jump { get; set; }
        public bool Kick { get; set; }
    }
}