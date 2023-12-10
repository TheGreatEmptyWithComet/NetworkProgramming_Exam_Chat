﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClientApp
{
    public class MessageObject
    {
        public MessageStatus MessageStatus { get; set; }
        public string UserName { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public bool IsNativeOrigin { get; set; }
        public string UserNameColor { get; set; } = "#FF4680F1";
    }
}