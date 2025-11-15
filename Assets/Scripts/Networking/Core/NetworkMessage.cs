using System;
using UnityEngine;

namespace SimpleNetworking
{
    /// <summary>
    /// Base class for all network messages.
    /// Serializable for easy transmission over network.
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        /// <summary>
        /// Unique identifier for the sender client
        /// </summary>
        public string senderId;

        /// <summary>
        /// Type of message being sent
        /// </summary>
        public string messageType;

        /// <summary>
        /// Timestamp when message was sent
        /// </summary>
        public long timestamp;

        /// <summary>
        /// JSON payload containing message data
        /// </summary>
        public string payload;

        public NetworkMessage()
        {
            timestamp = DateTime.UtcNow.Ticks;
        }

        public NetworkMessage(string senderId, string messageType, string payload)
        {
            this.senderId = senderId;
            this.messageType = messageType;
            this.payload = payload;
            this.timestamp = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Serialize message to JSON string for transmission
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Deserialize message from JSON string
        /// </summary>
        public static NetworkMessage FromJson(string json)
        {
            return JsonUtility.FromJson<NetworkMessage>(json);
        }

        /// <summary>
        /// Get the age of this message in seconds
        /// </summary>
        public float GetAgeSeconds()
        {
            return (DateTime.UtcNow.Ticks - timestamp) / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Generic typed message for specific payload types
    /// </summary>
    [Serializable]
    public class NetworkMessage<T> where T : class
    {
        public string senderId;
        public string messageType;
        public long timestamp;
        public T data;

        public NetworkMessage()
        {
            timestamp = DateTime.UtcNow.Ticks;
        }

        public NetworkMessage(string senderId, string messageType, T data)
        {
            this.senderId = senderId;
            this.messageType = messageType;
            this.data = data;
            this.timestamp = DateTime.UtcNow.Ticks;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static NetworkMessage<T> FromJson(string json)
        {
            return JsonUtility.FromJson<NetworkMessage<T>>(json);
        }
    }
}
