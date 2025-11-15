using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleNetworking.Examples
{
    /// <summary>
    /// Example chat message data
    /// </summary>
    [System.Serializable]
    public class ChatMessageData
    {
        public string username;
        public string message;
        public long timestamp;
    }

    /// <summary>
    /// Simple chat example using SimpleNetworkMessenger.
    /// Demonstrates sending and receiving text messages between clients.
    /// </summary>
    public class ChatExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the network messenger")]
        public SimpleNetworkMessenger messenger;

        [Header("UI (Optional - for testing)")]
        public TMP_InputField inputField;
        public TMP_Text chatDisplay;
        public Button sendButton;

        [Header("Settings")]
        public string username = "Player";
        public int maxChatMessages = 50;

        private string chatLog = "";
        private int messageCount = 0;

        void Start()
        {
            // Get messenger if not assigned
            if (messenger == null)
            {
                messenger = GetComponent<SimpleNetworkMessenger>();
            }

            // Subscribe to message events
            if (messenger != null)
            {
                messenger.onMessageReceived.AddListener(OnMessageReceived);
            }

            // Setup UI
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SendChatMessage);
            }

            if (inputField != null)
            {
                inputField.onSubmit.AddListener((text) => SendChatMessage());
            }
        }

        /// <summary>
        /// Send a chat message
        /// </summary>
        public void SendChatMessage()
        {
            if (messenger == null || !messenger.IsConnected)
            {
                Debug.LogWarning("[ChatExample] Not connected to server");
                return;
            }

            string message = inputField != null ? inputField.text : "";
            if (string.IsNullOrEmpty(message)) return;

            // Create chat message data
            ChatMessageData chatData = new ChatMessageData
            {
                username = username,
                message = message,
                timestamp = System.DateTime.UtcNow.Ticks
            };

            // Send message
            messenger.SendMessage(MessageType.CHAT, chatData);

            // Display locally
            AddChatMessage($"{username}: {message}");

            // Clear input
            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            Debug.Log($"[ChatExample] Sent: {message}");
        }

        /// <summary>
        /// Send a chat message with custom text
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (messenger == null || !messenger.IsConnected)
            {
                Debug.LogWarning("[ChatExample] Not connected to server");
                return;
            }

            if (string.IsNullOrEmpty(message)) return;

            ChatMessageData chatData = new ChatMessageData
            {
                username = username,
                message = message,
                timestamp = System.DateTime.UtcNow.Ticks
            };

            messenger.SendMessage(MessageType.CHAT, chatData);
            AddChatMessage($"{username}: {message}");
        }

        /// <summary>
        /// Handle received messages
        /// </summary>
        private void OnMessageReceived(NetworkMessage message)
        {
            if (message.messageType == MessageType.CHAT)
            {
                try
                {
                    ChatMessageData chatData = JsonUtility.FromJson<ChatMessageData>(message.payload);
                    string displayMessage = $"{chatData.username}: {chatData.message}";
                    AddChatMessage(displayMessage);
                    Debug.Log($"[ChatExample] Received: {displayMessage}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ChatExample] Failed to parse chat message: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Add message to chat display
        /// </summary>
        private void AddChatMessage(string message)
        {
            messageCount++;

            // Add to log
            if (string.IsNullOrEmpty(chatLog))
            {
                chatLog = message;
            }
            else
            {
                chatLog += "\n" + message;
            }

            // Limit message count
            if (messageCount > maxChatMessages)
            {
                int firstNewline = chatLog.IndexOf('\n');
                if (firstNewline >= 0)
                {
                    chatLog = chatLog.Substring(firstNewline + 1);
                }
                messageCount--;
            }

            // Update display
            if (chatDisplay != null)
            {
                chatDisplay.text = chatLog;
            }
        }

        /// <summary>
        /// Clear chat display
        /// </summary>
        public void ClearChat()
        {
            chatLog = "";
            messageCount = 0;
            if (chatDisplay != null)
            {
                chatDisplay.text = "";
            }
        }
    }
}
