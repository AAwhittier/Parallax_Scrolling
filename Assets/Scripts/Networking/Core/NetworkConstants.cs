namespace SimpleNetworking
{
    /// <summary>
    /// Network configuration constants
    /// </summary>
    public static class NetworkConstants
    {
        // Default ports
        public const int DEFAULT_SERVER_PORT = 7777;
        public const int DEFAULT_DISCOVERY_PORT = 7778;

        // Buffer sizes
        public const int BUFFER_SIZE = 8192;
        public const int MAX_MESSAGE_SIZE = 4096;

        // Timeouts (in seconds)
        public const float CONNECTION_TIMEOUT = 10f;
        public const float HEARTBEAT_INTERVAL = 5f;
        public const float DISCOVERY_TIMEOUT = 5f;

        // Retry settings
        public const int MAX_RECONNECT_ATTEMPTS = 5;
        public const float RECONNECT_DELAY = 2f;
    }
}
