using System;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Player input data sent from client to server
    /// </summary>
    [Serializable]
    public class PlayerInput
    {
        // Sequence number for reconciliation
        public int sequenceId;

        // Timestamp
        public long timestamp;

        // Movement input (-1 to 1)
        public float moveX;
        public float moveY;

        // Action buttons
        public bool jump;
        public bool attack;
        public bool special;
        public bool interact;

        // Delta time (for variable framerate clients)
        public float deltaTime;

        public PlayerInput()
        {
            timestamp = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Serialize to JSON
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        /// <summary>
        /// Deserialize from JSON
        /// </summary>
        public static PlayerInput FromJson(string json)
        {
            return JsonUtility.FromJson<PlayerInput>(json);
        }

        /// <summary>
        /// Get input as Vector2
        /// </summary>
        public Vector2 GetMoveVector()
        {
            return new Vector2(moveX, moveY).normalized;
        }

        /// <summary>
        /// Check if any action button pressed
        /// </summary>
        public bool HasAction()
        {
            return jump || attack || special || interact;
        }
    }

    /// <summary>
    /// Input message wrapper
    /// </summary>
    [Serializable]
    public class InputMessage
    {
        public string playerId;
        public PlayerInput input;

        public InputMessage(string playerId, PlayerInput input)
        {
            this.playerId = playerId;
            this.input = input;
        }
    }
}
