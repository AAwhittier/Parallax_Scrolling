namespace SimpleNetworking
{
    /// <summary>
    /// Common message type constants.
    /// You can add your own custom types as needed.
    /// </summary>
    public static class MessageType
    {
        // System Messages
        public const string CONNECT = "CONNECT";
        public const string DISCONNECT = "DISCONNECT";
        public const string PING = "PING";
        public const string PONG = "PONG";
        public const string HEARTBEAT = "HEARTBEAT";

        // Discovery Messages
        public const string DISCOVER_REQUEST = "DISCOVER_REQUEST";
        public const string DISCOVER_RESPONSE = "DISCOVER_RESPONSE";

        // Chat Messages
        public const string CHAT = "CHAT";
        public const string WHISPER = "WHISPER";

        // Game State Messages
        public const string POSITION_UPDATE = "POSITION_UPDATE";
        public const string ROTATION_UPDATE = "ROTATION_UPDATE";
        public const string TRANSFORM_UPDATE = "TRANSFORM_UPDATE";
        public const string STATE_UPDATE = "STATE_UPDATE";

        // Command Messages
        public const string COMMAND = "COMMAND";
        public const string EVENT = "EVENT";
        public const string CUSTOM = "CUSTOM";
    }
}
