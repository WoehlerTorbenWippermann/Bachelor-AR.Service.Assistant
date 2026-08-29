namespace Assets.Scripts.MyScripts.AssistanceSystem
{
    using Assets.Scripts.MyScripts.SpeechHandler;
    using Assets.Scripts.MyScripts.UiScripts;
    using NativeWebSocket;
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Text;
    using UnityEngine;
    using TMPro;
    using UnityEngine.Networking;
    using UnityEngine.UI;

#if WINDOWS_UWP
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.Storage.Streams;
    using System.Threading.Tasks;
#endif

    public enum AssistanceMode
    {
        AiAssistance,
        HumanAssistance
    }

    /// <summary>
    /// Central component for AI/human assistance via the messaging server.
    ///
    /// Flow on button press:
    ///   1. Capture a camera image (MediaCapture on HoloLens, WebCamTexture in the Editor)
    ///   2. Start dictation – the user speaks the question
    ///   3. After dictation: send a binary frame to the messaging server
    ///   4. Receive the response:
    ///        – Download the image (image_url) and show it as a floating panel
    ///        – Download the audio (audio_url) and play it
    /// </summary>
    public class AssistanceManager : MonoBehaviour
    {
        public static AssistanceManager Instance { get; private set; }

        [Header("Server")]
        [Tooltip("WebSocket URL of the TCP.Messaging.Server (format: ws://host:port).")]
        [SerializeField] private string serverUrl = "";
        [SerializeField] private string sessionId;

        [Header("Mode")]
        [SerializeField] private AssistanceMode currentMode = AssistanceMode.AiAssistance;

        [Header("Camera")]
        [Tooltip("JPEG quality for image compression (1–100). Only relevant for WebCamTexture in the Editor.")]
        [Range(1, 100)]
        [SerializeField] private int jpgQuality = 75;

        [Header("Image Panel")]
        [Tooltip("ImagePanelController – controls show/hide of the response panel.")]
        [SerializeField] private ImagePanelController imagePanelController;
        [Tooltip("Optional panel root (alternative to the ImagePanelController): hidden on start " +
                 "and shown on a response. For scenes without an assigned ImagePanelController (e.g. Tutorial).")]
        [SerializeField] private GameObject answerPanelRoot;
        [Tooltip("The RawImage element 'Annotated Image' inside the panel.")]
        [SerializeField] private RawImage annotatedImage;
        [Tooltip("The TMP_Text element 'Paragraph' inside the panel for the response text.")]
        [SerializeField] private TMP_Text responseParagraph;

        [Header("Audio")]
        [Tooltip("AudioSource for playing the downloaded response audio file.")]
        [SerializeField] private AudioSource responseAudioSource;
        [Tooltip("Camera shutter sound played after the image capture.")]
        [SerializeField] private AudioClip captureSound;
        [Tooltip("AudioSource for the processing sound (loop) played while waiting for a response.")]
        [SerializeField] private AudioSource processingAudioSource;

        [Header("Speech (optional – resolved via FindObjectOfType)")]
        [SerializeField] private MyDictationHandler dictationHandler;
        [SerializeField] private MyTextToSpeechHandler textToSpeechHandler;

        // ── Internal state machine ────────────────────────────────────────────
        private enum State { Idle, CapturingImage, ListeningForQuestion, SendingRequest, WaitingForResponse }
        private State _state = State.Idle;

        // ── Measurements (user study) ─────────────────────────────────────────
        private int _requestCount = 0;
        private float _requestSentTime = -1f;

        // ── Runtime objects ───────────────────────────────────────────────────
        private WebSocket _websocket;

#if WINDOWS_UWP
        private MediaCapture _mediaCapture;
        private bool _mediaCaptureReady = false;
#else
        // Editor fallback: WebCamTexture
        private WebCamTexture _webCamTexture;
        private Texture2D _frameTexture;
#endif

        private byte[] _capturedImageBytes;
        private string _latestRecognizedText = string.Empty;

        // ── Watchdog: prevents a stuck state (e.g. blocked speech recognition or
        //    a failed dictation) from blocking the state permanently and thereby
        //    ignoring "help" (OnCaptureButtonPressed only reacts in Idle).
        [Header("Watchdog (Safety)")]
        [Tooltip("Max. duration in state CapturingImage (countdown + photo), seconds.")]
        [SerializeField] private float capturingTimeoutSeconds = 20f;
        [Tooltip("Max. duration in state ListeningForQuestion (dictation), seconds.")]
        [SerializeField] private float listeningTimeoutSeconds = 30f;
        [Tooltip("Max. duration in state SendingRequest, seconds.")]
        [SerializeField] private float sendingTimeoutSeconds = 15f;
        [Tooltip("Max. duration in state WaitingForResponse (server response), seconds.")]
        [SerializeField] private float _responseTimeoutSeconds = 60f;

        private State _lastObservedState = State.Idle;
        private float _stateEnteredTime = -1f;

        // ── Reconnect (WebSocket) ─────────────────────────────────────────────
        [Header("Reconnect")]
        [Tooltip("Base wait time before the first reconnect attempt, seconds (exponential backoff).")]
        [SerializeField] private float reconnectBaseDelaySeconds = 2f;
        [Tooltip("Upper bound of the reconnect wait time, seconds.")]
        [SerializeField] private float reconnectMaxDelaySeconds = 30f;
        private bool _intentionalClose = false;
        private int _reconnectAttempts = 0;
        private Coroutine _reconnectRoutine = null;

        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
            imagePanelController?.ClosePanel();
            if (answerPanelRoot != null)
                answerPanelRoot.SetActive(false);
        }

        private void Start()
        {

            // The session ID is now generated by the messaging server on its start
            // and transmitted on connect (see HandleIncomingMessage → "session").
            Debug.Log("[AssistanceManager] Waiting for session ID from server...");

            if (dictationHandler == null)
                dictationHandler = FindObjectOfType<MyDictationHandler>();
            if (textToSpeechHandler == null)
                textToSpeechHandler = FindObjectOfType<MyTextToSpeechHandler>();

            if (dictationHandler == null)
                Debug.LogError("[AssistanceManager] MyDictationHandler not found!");
            if (textToSpeechHandler == null)
                Debug.LogError("[AssistanceManager] MyTextToSpeechHandler not found!");

            if (responseAudioSource == null)
                responseAudioSource = gameObject.AddComponent<AudioSource>();

            StartCoroutine(InitCamera());

            if (dictationHandler != null)
            {
                dictationHandler.OnSpeechRecognized.AddListener(OnSpeechRecognized);
                dictationHandler.OnRecognitionFinished.AddListener(OnRecognitionFinished);
                dictationHandler.OnRecognitionFaulted.AddListener(OnRecognitionFaulted);
            }

            ConnectToServer();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket?.DispatchMessageQueue();
#endif
            while (_mainThreadQueue.TryDequeue(out Action action))
                action?.Invoke();

            UpdateStateWatchdog();
        }

        /// <summary>
        /// Automatically resets the state to Idle when an active state lasts too
        /// long (e.g. because the dictation is stuck or no server response arrives).
        /// Without this watchdog, "help" stayed permanently ineffective after a stuck
        /// dictation, because <see cref="OnCaptureButtonPressed"/> only reacts in Idle.
        /// </summary>
        private void UpdateStateWatchdog()
        {
            if (_state != _lastObservedState)
            {
                _lastObservedState = _state;
                _stateEnteredTime = Time.time;
            }

            if (_state == State.Idle || _stateEnteredTime < 0f)
                return;

            float limit;
            switch (_state)
            {
                case State.CapturingImage:       limit = capturingTimeoutSeconds; break;
                case State.ListeningForQuestion: limit = listeningTimeoutSeconds; break;
                case State.SendingRequest:       limit = sendingTimeoutSeconds;   break;
                case State.WaitingForResponse:   limit = _responseTimeoutSeconds; break;
                default:                         limit = -1f;                     break;
            }

            if (limit <= 0f)
                return;

            if (Time.time - _stateEnteredTime > limit)
            {
                Debug.LogWarning($"[AssistanceManager] Watchdog: state {_state} exceeded {limit:F0}s – resetting to Idle.");
                AbortToIdle();
            }
        }

        /// <summary>
        /// Safely aborts the current flow and returns to the Idle state.
        /// Stops any running dictation so the keyword recognition becomes free again.
        /// </summary>
        private void AbortToIdle()
        {
            processingAudioSource?.Stop();

            if (_state == State.ListeningForQuestion || _state == State.CapturingImage)
                dictationHandler?.StopRecognition();

            _state = State.Idle;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _intentionalClose = true;   // no reconnect on destroy
            _ = _websocket?.Close();
#if WINDOWS_UWP
            _mediaCapture?.Dispose();
#endif
        }

        private void OnApplicationQuit()
        {
            if (dictationHandler != null)
            {
                dictationHandler.OnSpeechRecognized.RemoveListener(OnSpeechRecognized);
                dictationHandler.OnRecognitionFinished.RemoveListener(OnRecognitionFinished);
                dictationHandler.OnRecognitionFaulted.RemoveListener(OnRecognitionFaulted);
            }

            _intentionalClose = true;   // no reconnect on app quit
            _ = _websocket?.Close();

#if WINDOWS_UWP
            _mediaCapture?.Dispose();
#else
            if (_webCamTexture != null && _webCamTexture.isPlaying)
                _webCamTexture.Stop();
#endif
        }

        // ── Camera initialization ─────────────────────────────────────────────

        private IEnumerator InitCamera()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogError("[AssistanceManager] Camera access denied.");
                yield break;
            }

#if WINDOWS_UWP
            bool initDone = false;
            InitMediaCaptureAsync().ContinueWith(_ => initDone = true);
            yield return new WaitUntil(() => initDone);
#else
            // Editor fallback: WebCamTexture
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("[AssistanceManager] No camera found.");
                yield break;
            }

            Debug.Log($"[AssistanceManager] Available cameras ({devices.Length}):");
            for (int i = 0; i < devices.Length; i++)
                Debug.Log($"[AssistanceManager]   [{i}] {devices[i].name} | Front={devices[i].isFrontFacing}");

            string bestDevice = devices[0].name;
            foreach (var dev in devices)
            {
                if (!dev.isFrontFacing) { bestDevice = dev.name; break; }
            }

            _webCamTexture = new WebCamTexture(bestDevice, 1280, 720, 30);
            _webCamTexture.Play();
            _frameTexture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            Debug.Log($"[AssistanceManager] WebCamTexture started: {bestDevice}");
#endif
        }

#if WINDOWS_UWP
        private async Task InitMediaCaptureAsync()
        {
            try
            {
                // Log all available cameras
                var deviceList = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                    Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                Debug.Log($"[AssistanceManager] Available cameras ({deviceList.Count}):");
                foreach (var d in deviceList)
                    Debug.Log($"[AssistanceManager]   {d.Name} | Id={d.Id}");

                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    // Leave VideoDeviceId empty → system picks the default rear camera
                };
                await _mediaCapture.InitializeAsync(settings);

                // Log supported photo resolutions
                var photoProps = _mediaCapture.VideoDeviceController
                    .GetAvailableMediaStreamProperties(MediaStreamType.Photo);
                Debug.Log($"[AssistanceManager] Supported photo resolutions ({photoProps.Count}):");
                foreach (var p in photoProps)
                {
                    if (p is ImageEncodingProperties img)
                        Debug.Log($"[AssistanceManager]   {img.Width}x{img.Height} {img.Subtype}");
                }

                _mediaCaptureReady = true;
                Debug.Log("[AssistanceManager] MediaCapture initialized (highest quality active).");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssistanceManager] MediaCapture initialization failed: {e.Message}");
            }
        }

        private async Task<byte[]> TakePhotoAsync()
        {
            // Pick the highest available photo resolution
            var props = _mediaCapture.VideoDeviceController
                .GetAvailableMediaStreamProperties(MediaStreamType.Photo);

            ImageEncodingProperties bestProps = null;
            uint bestPixels = 0;
            foreach (var p in props)
            {
                if (p is ImageEncodingProperties img)
                {
                    uint pixels = img.Width * img.Height;
                    if (pixels > bestPixels)
                    {
                        bestPixels = pixels;
                        bestProps = img;
                    }
                }
            }

            if (bestProps != null)
            {
                await _mediaCapture.VideoDeviceController
                    .SetMediaStreamPropertiesAsync(MediaStreamType.Photo, bestProps);
                Debug.Log($"[AssistanceManager] Photo resolution set: {bestProps.Width}x{bestProps.Height}");
            }

            var jpegProps = ImageEncodingProperties.CreateJpeg();
            using (var stream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(jpegProps, stream);
                stream.Seek(0);
                var bytes = new byte[stream.Size];
                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                }
                Debug.Log($"[AssistanceManager] Photo captured ({bytes.Length} bytes, {bestProps?.Width}x{bestProps?.Height})");
                return bytes;
            }
        }
#endif

        // ── Public API ────────────────────────────────────────────────────────

        public void OnCaptureButtonPressed()
        {
            if (_state != State.Idle)
            {
                Debug.LogWarning("[AssistanceManager] Already active – request ignored.");
                return;
            }

            Debug.Log("[AssistanceManager] Capture button pressed – countdown starts");
            _state = State.CapturingImage;
            StartCoroutine(CountdownThenCapture());
        }

        private IEnumerator CountdownThenCapture()
        {
            textToSpeechHandler?.SpeakNumber("3");
            yield return new WaitForSeconds(0.65f);

            textToSpeechHandler?.SpeakNumber("2");
            yield return new WaitForSeconds(0.65f);

            textToSpeechHandler?.SpeakNumber("1");
            yield return new WaitForSeconds(0.65f);

            // Deliberately do NOT reset the state to Idle here: otherwise a one-frame
            // window opens in which another "help" could start a second, parallel
            // capture/dictation session (speech collision). The state stays active
            // throughout; CaptureImageAndStartDictation sets it to CapturingImage anyway.
            StartCoroutine(CaptureImageAndStartDictation());
        }

        public static event Action<AssistanceMode> OnModeChanged;

        public AssistanceMode CurrentMode => currentMode;

        public void SetAiMode()
        {
            currentMode = AssistanceMode.AiAssistance;
            Debug.Log("[AssistanceManager] Mode: AiAssistance");
            OnModeChanged?.Invoke(currentMode);
        }

        public void SetHumanMode()
        {
            currentMode = AssistanceMode.HumanAssistance;
            Debug.Log("[AssistanceManager] Mode: HumanAssistance");
            OnModeChanged?.Invoke(currentMode);
        }

        public void SetMode(bool isAi)
        {
            currentMode = isAi ? AssistanceMode.AiAssistance : AssistanceMode.HumanAssistance;
            OnModeChanged?.Invoke(currentMode);
        }

        // ── Capture + dictation ───────────────────────────────────────────────

        private IEnumerator CaptureImageAndStartDictation()
        {
            _state = State.CapturingImage;

#if WINDOWS_UWP
            if (!_mediaCaptureReady)
            {
                Debug.LogError("[AssistanceManager] MediaCapture not ready.");
                _state = State.Idle;
                yield break;
            }

            byte[] photoBytes = null;
            bool done = false;

            TakePhotoAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                    Debug.LogError($"[AssistanceManager] Photo failed: {t.Exception.Message}");
                else
                    photoBytes = t.Result;
                done = true;
            });

            yield return new WaitUntil(() => done);

            if (photoBytes == null)
            {
                Debug.LogError("[AssistanceManager] No photo received.");
                _state = State.Idle;
                yield break;
            }

            _capturedImageBytes = photoBytes;
#else
            // Editor fallback: WebCamTexture
            if (_webCamTexture == null || !_webCamTexture.isPlaying)
            {
                Debug.LogError("[AssistanceManager] Camera not available.");
                _state = State.Idle;
                yield break;
            }

            int w = _webCamTexture.width;
            int h = _webCamTexture.height;
            if (_frameTexture == null || _frameTexture.width != w || _frameTexture.height != h)
            {
                if (_frameTexture != null) Destroy(_frameTexture);
                _frameTexture = new Texture2D(w, h, TextureFormat.RGB24, false);
                Debug.Log($"[AssistanceManager] Camera resolution: {w}x{h}");
            }

            _frameTexture.SetPixels(_webCamTexture.GetPixels());
            _frameTexture.Apply();
            _capturedImageBytes = _frameTexture.EncodeToJPG(jpgQuality);
            Debug.Log($"[AssistanceManager] Image captured ({_capturedImageBytes.Length} bytes)");
#endif

            if (captureSound != null)
                responseAudioSource.PlayOneShot(captureSound);

            _latestRecognizedText = string.Empty;
            _state = State.ListeningForQuestion;
            dictationHandler?.StartRecognition();
        }

        // ── Dictation callbacks ───────────────────────────────────────────────

        private void OnSpeechRecognized(string text)
        {
            if (_state != State.ListeningForQuestion)
                return;

            string question = text.Trim();
            if (string.IsNullOrEmpty(question))
                return;

            _latestRecognizedText = question;

            Debug.Log($"[AssistanceManager] Question recognized – sending immediately: \"{question}\"");
            SendRequestToServer(question, _capturedImageBytes);
            dictationHandler?.StopRecognition();
        }

        private void OnRecognitionFinished(string reason)
        {
            if (_state != State.ListeningForQuestion)
                return;

            string question = _latestRecognizedText.Trim();
            if (string.IsNullOrEmpty(question))
                question = "Was siehst du hier?";

            Debug.Log($"[AssistanceManager] Question via timeout (fallback): \"{question}\"");
            SendRequestToServer(question, _capturedImageBytes);
        }

        private void OnRecognitionFaulted(string reason)
        {
            if (_state == State.ListeningForQuestion)
            {
                Debug.LogWarning("[AssistanceManager] Dictation failed: " + reason);
                _state = State.Idle;
            }
        }

        // ── Send request ──────────────────────────────────────────────────────

        private async void SendRequestToServer(string question, byte[] imageBytes)
        {
            if (_websocket == null || _websocket.State != WebSocketState.Open)
            {
                Debug.LogError("[AssistanceManager] Not connected – request aborted.");
                _state = State.Idle;
                return;
            }

            _state = State.SendingRequest;

            var lang = Assets.Scripts.MyScripts.UiScripts.LocalizationManager.Instance != null
                ? Assets.Scripts.MyScripts.UiScripts.LocalizationManager.Instance.CurrentLanguage.ToString().ToLower()
                : "german";

            var meta = new WsRequestMessage
            {
                type = "request",
                sessionId = sessionId,
                question = question,
                assistanceMode = currentMode.ToString(),
                language = lang
            };

            byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(meta));

            byte[] lenBytes = new byte[4];
            int jsonLen = jsonBytes.Length;
            lenBytes[0] = (byte)((jsonLen >> 24) & 0xFF);
            lenBytes[1] = (byte)((jsonLen >> 16) & 0xFF);
            lenBytes[2] = (byte)((jsonLen >> 8)  & 0xFF);
            lenBytes[3] = (byte)(jsonLen          & 0xFF);

            byte[] frame = new byte[4 + jsonBytes.Length + imageBytes.Length];
            System.Buffer.BlockCopy(lenBytes,   0, frame, 0,                    4);
            System.Buffer.BlockCopy(jsonBytes,  0, frame, 4,                    jsonBytes.Length);
            System.Buffer.BlockCopy(imageBytes, 0, frame, 4 + jsonBytes.Length, imageBytes.Length);

            Debug.Log($"[AssistanceManager] Sending {meta.assistanceMode} | Question: \"{question}\" | Frame: {frame.Length} bytes");

            await _websocket.Send(frame);

            _requestCount++;
            _requestSentTime = Time.realtimeSinceStartup;
            Debug.Log($"[STUDY] Request #{_requestCount} sent | Mode={meta.assistanceMode} | Language={meta.language} | Question=\"{question}\"");

            processingAudioSource?.Play();
            _state = State.WaitingForResponse;
        }

        // ── Process response ──────────────────────────────────────────────────

        private void HandleIncomingMessage(string json)
        {
            try
            {
                Debug.Log($"[AssistanceManager] Message received (State={_state}): {json}");

                var basic = JsonUtility.FromJson<WsBasicMessage>(json);
                if (basic?.type == "ping")
                {
                    _ = _websocket.SendText(JsonUtility.ToJson(new WsBasicMessage { type = "pong" }));
                    return;
                }

                // Adopt and store the session ID assigned by the server
                if (basic?.type == "session")
                {
                    var sess = JsonUtility.FromJson<WsSessionMessage>(json);
                    if (!string.IsNullOrEmpty(sess?.sessionId))
                    {
                        sessionId = sess.sessionId;
                        Debug.Log($"[AssistanceManager] Session ID received from server: {sessionId}");
                    }
                    return;
                }

                var response = JsonUtility.FromJson<AssistanceResponse>(json);

                if (response == null || (string.IsNullOrEmpty(response.answer)
                    && string.IsNullOrEmpty(response.image_url)
                    && string.IsNullOrEmpty(response.audio_url)))
                {
                    Debug.Log("[AssistanceManager] Message ignored (no answer/image_url/audio_url).");
                    return;
                }

                processingAudioSource?.Stop();
                float responseTime = _requestSentTime >= 0f ? Time.realtimeSinceStartup - _requestSentTime : -1f;
                _requestSentTime = -1f;
                Debug.Log($"[STUDY] Response #{_requestCount} received | ServerTime={responseTime:F2}s | Answer=\"{response.answer}\"");
                Debug.Log($"[AssistanceManager] Response received: \"{response.answer}\"");

                if (responseParagraph != null && !string.IsNullOrEmpty(response.answer))
                    responseParagraph.text = response.answer;

                // Show the panel immediately – keep the image element hidden until the download finishes
                if (annotatedImage != null)
                    annotatedImage.gameObject.SetActive(false);
                if (answerPanelRoot != null)
                    answerPanelRoot.SetActive(true);
                imagePanelController?.ShowPanel();

                if (!string.IsNullOrEmpty(response.image_url))
                    StartCoroutine(DownloadAndShowImage(FixLocalhostUrl(response.image_url)));

                if (!string.IsNullOrEmpty(response.audio_url))
                    StartCoroutine(DownloadAndPlayAudio(FixLocalhostUrl(response.audio_url), response.answer));
                else if (!string.IsNullOrEmpty(response.answer))
                    textToSpeechHandler?.SpeakAnswer(response.answer);

                _state = State.Idle;
            }
            catch (Exception e)
            {
                Debug.LogError("[AssistanceManager] Error while processing the response: " + e);
                _state = State.Idle;
            }
        }

        // ── WebSocket ─────────────────────────────────────────────────────────

        private async void ConnectToServer()
        {
            Debug.Log($"[AssistanceManager] Connecting to {serverUrl}...");
            _websocket = new WebSocket(serverUrl);

            _websocket.OnOpen += () =>
            {
                Debug.Log("[AssistanceManager] Connected");
                _reconnectAttempts = 0;   // successful connection → reset backoff
                SendRegistration();
            };

            _websocket.OnError += (e) =>
                Debug.LogError("[AssistanceManager] WebSocket error: " + e);

            _websocket.OnClose += (e) =>
            {
                Debug.Log($"[AssistanceManager] Connection lost ({e})");
                if (!_intentionalClose)
                    _mainThreadQueue.Enqueue(ScheduleReconnect);
            };

            _websocket.OnMessage += (bytes) =>
            {
                string json = Encoding.UTF8.GetString(bytes);
                _mainThreadQueue.Enqueue(() => HandleIncomingMessage(json));
            };

            try
            {
                await _websocket.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AssistanceManager] Connection attempt failed: {ex.Message}");
                if (!_intentionalClose)
                    _mainThreadQueue.Enqueue(ScheduleReconnect);
            }
        }

        // ── Reconnect with exponential backoff ────────────────────────────────

        /// <summary>
        /// Schedules a reconnect attempt (only one at a time). Called via the
        /// main-thread queue so that StartCoroutine runs safely.
        /// </summary>
        private void ScheduleReconnect()
        {
            if (_intentionalClose) return;
            if (_reconnectRoutine != null) return;   // an attempt is already scheduled
            _reconnectRoutine = StartCoroutine(ReconnectAfterDelay());
        }

        private IEnumerator ReconnectAfterDelay()
        {
            _reconnectAttempts++;
            float delay = Mathf.Min(
                reconnectBaseDelaySeconds * Mathf.Pow(2f, _reconnectAttempts - 1),
                reconnectMaxDelaySeconds);
            Debug.Log($"[AssistanceManager] Reconnect attempt {_reconnectAttempts} in {delay:F0}s...");

            yield return new WaitForSeconds(delay);

            _reconnectRoutine = null;
            if (_intentionalClose) yield break;

            ConnectToServer();   // async void – closes again with ScheduleReconnect on another error
        }

        private async void SendRegistration()
        {
            string msg = JsonUtility.ToJson(new WsRegisterMessage { type = "register", role = "unity" });
            await _websocket.SendText(msg);
            Debug.Log("[AssistanceManager] Registered as 'unity'");
        }

        // ── URL helper ────────────────────────────────────────────────────────

        private string FixLocalhostUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            string serverHost = "localhost";
            try
            {
                string trimmed = serverUrl.Replace("ws://", "").Replace("wss://", "");
                serverHost = trimmed.Split(':')[0];
            }
            catch { }

            string fixedUrl = url.Replace("localhost", serverHost);
            if (fixedUrl != url)
                Debug.Log($"[AssistanceManager] URL corrected: {url} → {fixedUrl}");

            return fixedUrl;
        }

        // ── Image download and panel ──────────────────────────────────────────

        private IEnumerator DownloadAndShowImage(string url)
        {
            Debug.Log($"[AssistanceManager] Downloading image from: {url}");

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[AssistanceManager] Image download failed: {req.error} – panel stays visible without image.");
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(req);
                ShowImagePanel(texture);
            }
        }

        private void ShowImagePanel(Texture2D texture)
        {
            if (annotatedImage != null)
            {
                annotatedImage.texture = texture;
                annotatedImage.gameObject.SetActive(true);
                annotatedImage.GetComponent<UiScripts.ImageZoomOnClick>()?.SyncCollider();
            }
            else
                Debug.LogWarning("[AssistanceManager] 'Annotated Image' is not set.");

            if (answerPanelRoot != null)
                answerPanelRoot.SetActive(true);
            imagePanelController?.ShowPanel();
            Debug.Log($"[AssistanceManager] Image panel shown ({texture.width}x{texture.height})");
        }

        // ── Audio download and playback ───────────────────────────────────────

        private IEnumerator DownloadAndPlayAudio(string url, string fallbackAnswer = null)
        {
            Debug.Log($"[AssistanceManager] Downloading audio from: {url}");

            AudioType[] typesToTry = { AudioType.MPEG, AudioType.WAV, AudioType.UNKNOWN };

            foreach (AudioType audioType in typesToTry)
            {
                using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
                {
                    yield return req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[AssistanceManager] Audio download failed ({audioType}): {req.error}");
                        continue;
                    }

                    AudioClip clip = null;
                    try { clip = DownloadHandlerAudioClip.GetContent(req); } catch { }

                    if (clip == null || clip.length <= 0f)
                    {
                        Debug.LogWarning($"[AssistanceManager] AudioClip invalid ({audioType}) – next attempt.");
                        continue;
                    }

                    responseAudioSource.clip = clip;
                    responseAudioSource.Play();
                    Debug.Log($"[AssistanceManager] Audio is playing ({clip.length:F1}s, type={audioType})");
                    yield break;
                }
            }

            Debug.LogWarning("[AssistanceManager] Audio could not be loaded – TTS fallback.");
            if (!string.IsNullOrEmpty(fallbackAnswer))
                textToSpeechHandler?.SpeakAnswer(fallbackAnswer);
        }
    }

    // ── Serializable message structures ───────────────────────────────────────

    [Serializable]
    public class WsRegisterMessage
    {
        public string type;
        public string role;
    }

    [Serializable]
    public class WsBasicMessage
    {
        public string type;
    }

    [Serializable]
    public class WsSessionMessage
    {
        public string type;
        public string sessionId;
    }

    [Serializable]
    public class WsRequestMessage
    {
        public string type;
        public string sessionId;
        public string question;
        public string assistanceMode;
        public string language;
    }

    [Serializable]
    public class AssistanceResponse
    {
        public string session_id;
        public string answer;
        public string image_url;
        public string audio_url;
        public AssistanceBox[] boxes;
    }

    [Serializable]
    public class AssistanceBox
    {
        public string label;
        public int[] box_2d;
    }
}
