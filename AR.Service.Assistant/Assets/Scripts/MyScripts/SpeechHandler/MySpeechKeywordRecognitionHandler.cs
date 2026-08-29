// Copyright (c) Mixed Reality Toolkit Contributors
// Licensed under the BSD 3-Clause

using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.MyScripts.SpeechHandler
{
    /// <summary>
    /// A helper for registering keywords a <see cref="KeywordRecognitionSubsystem"/>.
    /// </summary>
    /// <remarks>
    /// When a <see cref="KeywordRecognitionSubsystem"/> recognizes one of the specified keywords, this
    /// will raise Unity events that consumers can respond too. See <see cref="KeywordEvent"/> for more
    /// details.
    /// </remarks>
    public class MySpeechKeywordRecognitionHandler : MonoBehaviour
    {
        /// <summary>
        /// A structure holding a Unity event that will be raised when the corresponding keyword is recognized.
        /// </summary>
        [Serializable]
        public struct KeywordEvent
        {
            /// <summary>
            /// The keyword that a <see cref="KeywordRecognitionSubsystem"/> will listen for.
            /// </summary>
            [SerializeField]
            [Tooltip("The keyword that the KeywordRecognitionSubsystem will listen for.")]
            public string Keyword;

            /// <summary>
            /// The event raised when a <see cref="KeywordRecognitionSubsystem"/> recognizes the <see cref="Keyword"/>.
            /// </summary>
            [SerializeField]
            [Tooltip("The event raised when a KeywordRecognitionSubsystem recognizes the Keyword.")]
            public UnityEvent Event;
        }

        [SerializeField]
        [Tooltip("Get or set the list of keywords that a KeywordRecognitionSubsystem will listen for.")]
        private List<KeywordEvent> keywords = new List<KeywordEvent>();

        /// <summary>
        /// Get or set the list of keywords that a <see cref="KeywordRecognitionSubsystem"/> will listen for. 
        /// </summary>
        public List<KeywordEvent> Keywords
        {
            get => keywords;
            set
            {
                keywords = value;
                UpdateKeywords();
            }
        }

        [SerializeField]
        [Tooltip("A Unity event that will be raised when any keyword is recognized.")]
        private UnityEvent globalEvent;

        [SerializeField]
        [Tooltip("Enables keyword recognition right at start – regardless of whether another script " +
                 "calls EnableKeywordRecognition(). Required when the activator (e.g. MyDialogActions) sits on an " +
                 "object that is inactive at start (Tutorial). In the AI/HI scene MyDialogActions handles this anyway.")]
        private bool enableOnStart = true;

        private KeywordRecognitionSubsystem keywordRecognitionSubsystem;
        private List<UnityEngine.Events.UnityAction> registeredActions = new List<UnityEngine.Events.UnityAction>();
        private bool isRecognitionEnabled = false;

        /// <summary>
        /// A Unity event function that is called on the frame when a script is enabled just before any of the update methods are called the first time.
        /// </summary> 
        private void Start()
        {
            keywordRecognitionSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<KeywordRecognitionSubsystem>();
            if (keywordRecognitionSubsystem == null)
                Debug.LogWarning("[KeywordRecognition] No KeywordRecognitionSubsystem found – keywords are not recognized.");
            else
                Debug.Log("[KeywordRecognition] Subsystem found and ready.");

            // Enable keywords from scene start, regardless of whether a (possibly inactive)
            // UI object runs its Awake. IMPORTANT: always call UpdateKeywords() afterwards –
            // during Awake the subsystem may still have been null, so the keywords are only
            // actually registered with the subsystem HERE (this was already the case before).
            if (enableOnStart)
                isRecognitionEnabled = true;

            UpdateKeywords();
        }

        private void UpdateKeywords()
        {
            // Remove all old listeners
            UnregisterAllKeywords();

            if (!isRecognitionEnabled || keywordRecognitionSubsystem == null)
            {
                return;
            }

            foreach (var data in keywords)
            {
                string kw = data.Keyword;
                UnityEngine.Events.UnityAction action = () =>
                {
                    Debug.Log($"[KeywordRecognition] Keyword recognized: \"{kw}\"");
                    globalEvent?.Invoke();
                    data.Event?.Invoke();
                };

                keywordRecognitionSubsystem.CreateOrGetEventForKeyword(kw).AddListener(action);
                registeredActions.Add(action);
            }
        }

        /// <summary>
        /// Enables keyword recognition.
        /// </summary>
        public void EnableKeywordRecognition()
        {
            if (!isRecognitionEnabled)
            {
                isRecognitionEnabled = true;
                UpdateKeywords();
                Debug.Log($"[KeywordRecognition] Started – {keywords.Count} keyword(s) registered: " +
                          string.Join(", ", keywords.ConvertAll(k => k.Keyword)));
            }
            else
            {
                Debug.Log("[KeywordRecognition] Was already active – no re-registration needed.");
            }
        }

        /// <summary>
        /// Disables keyword recognition.
        /// </summary>
        public void DisableKeywordRecognition()
        {
            if (isRecognitionEnabled)
            {
                isRecognitionEnabled = false;
                UnregisterAllKeywords();
                Debug.Log("[KeywordRecognition] Stopped – all keywords unregistered.");
            }
            else
            {
                Debug.Log("[KeywordRecognition] Was already inactive.");
            }
        }

        /// <summary>
        /// Returns whether keyword recognition is currently enabled.
        /// </summary>
        public bool IsRecognitionEnabled()
        {
            return isRecognitionEnabled;
        }

        /// <summary>
        /// After a stop/start of the KeywordRecognitionSubsystem (e.g. by the
        /// DictationHandler when switching to dictation) the subsystem instance may
        /// have changed or the keyword grammar may have been lost. This method grabs
        /// the currently running subsystem again and re-registers all keywords, so that
        /// "help" &amp; co. are reliably recognized again.
        /// Idempotent – can safely be called multiple times.
        /// </summary>
        public void RefreshSubsystemAndReRegister()
        {
            var subsys = XRSubsystemHelpers.GetFirstRunningSubsystem<KeywordRecognitionSubsystem>();
            if (subsys == null)
            {
                Debug.LogWarning("[KeywordRecognition] Re-register skipped – no running subsystem.");
                return;
            }

            if (subsys != keywordRecognitionSubsystem)
            {
                // Subsystem instance has changed → old listener references are invalid.
                registeredActions.Clear();
                keywordRecognitionSubsystem = subsys;
            }

            if (isRecognitionEnabled)
            {
                UpdateKeywords();
                Debug.Log("[KeywordRecognition] Keywords re-registered after dictation.");
            }
        }

        private void UnregisterAllKeywords()
        {
            if (keywordRecognitionSubsystem == null)
            {
                return;
            }

            // Remove all registered listeners
            for (int i = 0; i < keywords.Count && i < registeredActions.Count; i++)
            {
                var keyword = keywords[i].Keyword;
                var action = registeredActions[i];
                keywordRecognitionSubsystem.CreateOrGetEventForKeyword(keyword).RemoveListener(action);
            }

            registeredActions.Clear();
        }

        private void OnDestroy()
        {
            // Cleanup when the GameObject is destroyed
            UnregisterAllKeywords();
        }
    }
}
