// Copyright (c) Mixed Reality Toolkit Contributors
// Licensed under the BSD 3-Clause

// Disable "missing XML comment" warning for samples. While nice to have, this XML documentation is not required for samples.
#pragma warning disable CS1591

using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.MyScripts.SpeechHandler
{
    /// <summary>
    /// Demonstration script showing how to subscribe to and handle
    /// events fired by <see cref="DictationSubsystem"/>.
    /// </summary>
    [AddComponentMenu("MRTK/Examples/Dictation Handler")]
    public class MyDictationHandler : MonoBehaviour
    {
        /// <summary>
        /// Wrapper of UnityEvent&lt;string&gt; for serialization.
        /// </summary>
        [System.Serializable]
        public class StringUnityEvent : UnityEvent<string> { }

        /// <summary>
        /// Event raised while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnSpeechRecognizing { get; private set; }

        /// <summary>
        /// Event raised after the user pauses, typically at the end of a sentence. Contains the full recognized string so far.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnSpeechRecognized { get; private set; }

        /// <summary>
        /// Event raised when the recognizer stops. Contains the final recognized string.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnRecognitionFinished { get; private set; }

        /// <summary>
        /// Event raised when an error occurs. Contains the string representation of the error reason.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnRecognitionFaulted { get; private set; }

        private IDictationSubsystem dictationSubsystem = null;
        private IKeywordRecognitionSubsystem keywordRecognitionSubsystem = null;

        // KeywordRecognizer and DictationRecognizer CANNOT run at the same time on
        // Windows/UWP. Keyword recognition may only be restarted once no dictation
        // is active anymore – otherwise:
        // "Cannot start speech recognition system while a dictation recognition session is in progress!"
        private bool _dictationActive = false;

        // Idempotency guard: ensures that cleanup + keyword restart run EXACTLY ONCE
        // per dictation session – regardless of whether RecognitionFinished, Faulted,
        // a manual StopRecognition() or the fallback timer fires first.
        private bool _shutdownHandled = true;

        // The only safety net instead of retry/status polling: if, contrary to
        // expectation, no RecognitionFinished/Faulted fires after StopDictation(),
        // this timer cleans up.
        [SerializeField] private float fallbackShutdownSeconds = 3f;
        private Coroutine _fallbackRoutine = null;

        // After the keyword restart the keywords must be re-registered, because the
        // subsystem can lose its grammar through stop/start.
        private MySpeechKeywordRecognitionHandler _keywordHandler = null;

        /// <summary>
        /// Start dictation on a DictationSubsystem.
        /// </summary>
        public void StartRecognition()
        {
            // If, contrary to expectation, a dictation is still active: end it cleanly
            // and synchronously so we start from a clear state (normally a no-op).
            if (_dictationActive)
            {
                if (dictationSubsystem != null)
                    dictationSubsystem.StopDictation();
                HandleDictationShutdown();   // idempotent
            }
            StopFallbackTimer();

            dictationSubsystem = XRSubsystemHelpers.DictationSubsystem;
            if (dictationSubsystem == null)
            {
                OnRecognitionFaulted.Invoke("Cannot find a running DictationSubsystem. Please check the MRTK profile settings " +
                    "(Project Settings -> MRTK3) and/or ensure a DictationSubsystem is running.");
                return;
            }

            // On Windows/UWP the keyword and dictation recognizers may NOT run at the
            // same time → stop keyword recognition before the dictation.
            keywordRecognitionSubsystem = XRSubsystemHelpers.KeywordRecognitionSubsystem;
            keywordRecognitionSubsystem?.Stop();

            _dictationActive = true;
            _shutdownHandled = false;

            dictationSubsystem.Recognizing += DictationSubsystem_Recognizing;
            dictationSubsystem.Recognized += DictationSubsystem_Recognized;
            dictationSubsystem.RecognitionFinished += DictationSubsystem_RecognitionFinished;
            dictationSubsystem.RecognitionFaulted += DictationSubsystem_RecognitionFaulted;
            dictationSubsystem.StartDictation();
        }

        private void DictationSubsystem_RecognitionFaulted(DictationSessionEventArgs obj)
        {
            Debug.LogError("Dictation recognition faulted. Reason: " + obj.ReasonString);
            OnRecognitionFaulted.Invoke("Recognition faulted. Reason: " + obj.ReasonString);
            HandleDictationShutdown();
        }

        private void DictationSubsystem_RecognitionFinished(DictationSessionEventArgs obj)
        {
            Debug.Log("Dictation recognition finished. Reason: " + obj.ReasonString);
            OnRecognitionFinished.Invoke("Recognition finished. Reason: " + obj.ReasonString);
            HandleDictationShutdown();
        }

        private void DictationSubsystem_Recognized(DictationResultEventArgs obj)
        {
            Debug.Log("Dictation recognized: " + obj.Result);
            OnSpeechRecognized.Invoke(obj.Result);
        }

        private void DictationSubsystem_Recognizing(DictationResultEventArgs obj)
        {
            Debug.Log("Dictation recognizing: " + obj.Result);
            OnSpeechRecognizing.Invoke("Recognizing:" + obj.Result);
        }

        /// <summary>
        /// Ends the running dictation. Cleanup and keyword restart deliberately do NOT
        /// happen here, but in the RecognitionFinished/RecognitionFaulted callback (the
        /// real session end). Only this way is keyword recognition restarted only once
        /// the dictation session has provably ended – no guessing via timer/retry. A
        /// fallback timer covers the case where no end event arrives.
        /// </summary>
        public void StopRecognition()
        {
            if (_dictationActive && dictationSubsystem != null)
            {
                dictationSubsystem.StopDictation();
                StartFallbackTimer();
            }
        }

        /// <summary>
        /// Cleans up a dictation session EXACTLY ONCE (idempotent via <see cref="_shutdownHandled"/>)
        /// and re-enables keyword recognition. Called from RecognitionFinished/Faulted,
        /// from the fallback timer or (in the exceptional case) from StartRecognition.
        /// </summary>
        public void HandleDictationShutdown()
        {
            if (_shutdownHandled)
                return;
            _shutdownHandled = true;
            _dictationActive = false;

            StopFallbackTimer();

            if (dictationSubsystem != null)
            {
                dictationSubsystem.Recognizing -= DictationSubsystem_Recognizing;
                dictationSubsystem.Recognized -= DictationSubsystem_Recognized;
                dictationSubsystem.RecognitionFinished -= DictationSubsystem_RecognitionFinished;
                dictationSubsystem.RecognitionFaulted -= DictationSubsystem_RecognitionFaulted;
                dictationSubsystem = null;
            }

            // Dictation has now provably ended → re-enable keyword recognition.
            // Start() deliberately runs without status polling: the timing (session end)
            // is correct, so "dictation in progress" no longer occurs. If MRTK already
            // started the recognizer itself, an "already running" is harmless – hence the
            // try/catch so nothing aborts.
            if (keywordRecognitionSubsystem != null)
            {
                try { keywordRecognitionSubsystem.Start(); }
                catch (System.Exception e)
                {
                    Debug.Log($"[DictationHandler] keyword.Start() after dictation: {e.Message}");
                }
                keywordRecognitionSubsystem = null;
            }

            // Re-register keywords on the (possibly restarted) subsystem, otherwise the
            // recognizer runs but does not recognize "help".
            ReRegisterKeywords();

            Debug.Log("[DictationHandler] Dictation ended – keyword recognition active again.");
        }

        // ── Fallback timer: the only safety net against a missing end event ──

        private void StartFallbackTimer()
        {
            StopFallbackTimer();
            _fallbackRoutine = StartCoroutine(FallbackShutdown());
        }

        private void StopFallbackTimer()
        {
            if (_fallbackRoutine != null)
            {
                StopCoroutine(_fallbackRoutine);
                _fallbackRoutine = null;
            }
        }

        private System.Collections.IEnumerator FallbackShutdown()
        {
            yield return new UnityEngine.WaitForSeconds(fallbackShutdownSeconds);
            _fallbackRoutine = null;

            if (!_shutdownHandled)
            {
                Debug.LogWarning("[DictationHandler] No RecognitionFinished/Faulted after StopDictation – fallback cleanup triggered.");
                HandleDictationShutdown();
            }
        }

        /// <summary>
        /// Asks the keyword handler to grab the (possibly restarted) subsystem again
        /// and re-register all keywords.
        /// </summary>
        private void ReRegisterKeywords()
        {
            if (_keywordHandler == null)
                _keywordHandler = FindObjectOfType<MySpeechKeywordRecognitionHandler>();
            _keywordHandler?.RefreshSubsystemAndReRegister();
        }
    }
}
#pragma warning restore CS1591
