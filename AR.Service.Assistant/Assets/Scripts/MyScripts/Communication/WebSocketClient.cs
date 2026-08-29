namespace Assets.Scripts.MyScripts.Communication
{
    using Assets.Scripts.MyScripts.SpeechHandler;
    using NativeWebSocket;
    using System;
    using UnityEngine;

    public class WebSocketClient : MonoBehaviour
    {
        private WebSocket websocket;
        public string serverUrl = "";

        [Header("Text-to-Speech")]
        public MyTextToSpeechHandler textToSpeechHandler;

        private async void Start()
        {
            Debug.Log("Connecting to: " + serverUrl);

            websocket = new WebSocket(serverUrl);

            websocket.OnOpen += () =>
            {
                Debug.Log("✅ WebSocket connected");

                // Register as UNITY client
                Send(new Message
                {
                    type = "register",
                    role = "unity"
                });
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError("❌ WebSocket error: " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log("⚠️ Connection closed");
            };

            websocket.OnMessage += (bytes) =>
            {
                string message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("📩 Received: " + message);

                HandleMessage(message);
            };

            await websocket.Connect();
        }

        private void HandleMessage(string json)
        {
            try
            {
                Message msg = JsonUtility.FromJson<Message>(json);

                switch (msg.type)
                {
                    case "message":
                        Debug.Log("💬 Browser says: " + msg.data);
                        break;

                    case "chat_response":
                        HandleChatResponse(json);
                        break;

                    case "ping":
                        Send(new Message { type = "pong" });
                        break;

                    default:
                        Debug.Log("ℹ️ Unknown type: " + msg.type);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("❌ JSON error: " + e);
            }
        }

        private void HandleChatResponse(string json)
        {
            try
            {
                ChatResponseMessage chatResponse = JsonUtility.FromJson<ChatResponseMessage>(json);

                Debug.Log($"💬 Chat response received:");
                Debug.Log($"   User: {chatResponse.data.user}");
                Debug.Log($"   Assistant: {chatResponse.data.assistant}");

                // Text-to-speech for the assistant answer
                if (textToSpeechHandler != null)
                {
                    textToSpeechHandler.SpeakAnswer(chatResponse.data.assistant);
                }
                else
                {
                    Debug.LogWarning("⚠️ MyTextToSpeechHandler is not assigned!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Chat response parse error: " + e);
            }
        }

        public async void Send(Message msg)
        {
            if (websocket.State == WebSocketState.Open)
            {
                string json = JsonUtility.ToJson(msg);
                await websocket.SendText(json);
                Debug.Log("➡️ Sent: " + json);
            }
        }

        public async void SendChatMessage(string message)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                ChatMessage chatMsg = new ChatMessage
                {
                    type = "chat",
                    userId = "1",
                    message = message
                };

                string json = JsonUtility.ToJson(chatMsg);
                await websocket.SendText(json);
                Debug.Log("➡️ Chat sent: " + json);
            }
            else
            {
                Debug.LogWarning("⚠️ WebSocket not connected. Message not sent.");
            }
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            websocket.DispatchMessageQueue();
#endif
        }

        private async void OnApplicationQuit()
        {
            await websocket.Close();
        }
    }

    // ============================
    // DATA STRUCTURES
    // ============================

    [Serializable]
    public class Message
    {
        public string type;
        public string role;
        public string data;
    }

    [Serializable]
    public class ChatMessage
    {
        public string type;
        public string userId;
        public string message;
    }

    [Serializable]
    public class ChatResponseMessage
    {
        public string type;
        public ChatResponseData data;
    }

    [Serializable]
    public class ChatResponseData
    {
        public string user;
        public string assistant;
    }
}
