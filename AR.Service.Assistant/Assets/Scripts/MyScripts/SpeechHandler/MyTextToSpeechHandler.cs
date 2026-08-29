namespace Assets.Scripts.MyScripts.SpeechHandler
{
    using MixedReality.Toolkit;
    using MixedReality.Toolkit.Speech.Windows;
    using MixedReality.Toolkit.Subsystems;
    using UnityEngine;
    using UnityEngine.Events;
#if WINDOWS_UWP
    using Windows.Media.SpeechSynthesis;
#endif

    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("MRTK/Examples/TextToSpeech Handler")]
    public class MyTextToSpeechHandler : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The audio source where speech will be played.")]
        private AudioSource audioSource;

        /// <summary>
        /// Gets or sets the audio source where speech will be played.
        /// </summary>
        public AudioSource AudioSource
        {
            get { return audioSource; }
            set { audioSource = value; }
        }

        /// <summary>
        /// Gets or sets the voice that will be used to generate speech. To use a non en-US voice, set this to Other.
        /// </summary>
        public TextToSpeechVoice Voice
        {
            get { return voice; }
            set { voice = value; }
        }

        [Tooltip("The voice that will be used to generate speech. To use a non en-US voice, set this to Other.")]
        [SerializeField]
        private TextToSpeechVoice voice;

        /// <summary>
        /// Wrapper of UnityEvent&lt;string&gt; for serialization.
        /// </summary>
        [System.Serializable]
        public class StringUnityEvent : UnityEvent<string>
        {
        }

        /// <summary>
        /// Event raised when an error occurs. Contains the string representation of the error reason.
        /// </summary>
        [field: SerializeField]
        public StringUnityEvent OnSpeakFaulted { get; private set; }

        private TextToSpeechSubsystem textToSpeechSubsystem;

        /// <summary>
        /// Method to demonstrate the text to speech capability
        /// </summary>
        public void SpeakWelcome(bool enabled)
        {
            // If we have a text to speech manager on the target object, say something.
            // This voice will appear to emanate from the object.
            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
            if (textToSpeechSubsystem != null)
            {
                // profile associated with the Standalone build target group.
                MRTKProfile profile = MRTKProfile.Instance;

                if (profile == null)
                {
                    Debug.LogError("MRTK Profile could not be retrieved.");
                    return;
                }

                BaseSubsystemConfig config;

                // Attempt to retrieve the config associated with the indicated subsystem.
                if (!profile.TryGetConfigForSubsystem(typeof(WindowsTextToSpeechSubsystem), out config) || config == null)
                {
                    Debug.LogError($"Configuration could not be retrieved for {typeof(WindowsTextToSpeechSubsystem)}.", profile);
                }
                else
                {
                    WindowsTextToSpeechSubsystemConfig winConfig = config as WindowsTextToSpeechSubsystemConfig;
                    if (winConfig != null)
                    {
                        winConfig.Voice = voice;

                        var msg = string.Empty;

                        if (enabled)
                        {
                            msg = "Enabled";
                        }
                        else
                        {
                            msg = "Disabled";
                        }

                        // Speak message
                        textToSpeechSubsystem.TrySpeak(msg, audioSource);
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot find a running TextToSpeechSubsystem. Please check the MRTK profile settings " +
                               "(Project Settings -> MRTK3) and/or ensure a TextToSpeechSubsystem is running.");
                OnSpeakFaulted?.Invoke("Cannot find a running TextToSpeechSubsystem.");
            }
        }

        public void SpeakOkay()
        {
            // If we have a text to speech manager on the target object, say something.
            // This voice will appear to emanate from the object.
            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
            if (textToSpeechSubsystem != null)
            {
                // profile associated with the Standalone build target group.
                MRTKProfile profile = MRTKProfile.Instance;

                if (profile == null)
                {
                    Debug.LogError("MRTK Profile could not be retrieved.");
                    return;
                }

                BaseSubsystemConfig config;

                // Attempt to retrieve the config associated with the indicated subsystem.
                if (!profile.TryGetConfigForSubsystem(typeof(WindowsTextToSpeechSubsystem), out config) || config == null)
                {
                    Debug.LogError($"Configuration could not be retrieved for {typeof(WindowsTextToSpeechSubsystem)}.", profile);
                }
                else
                {
                    WindowsTextToSpeechSubsystemConfig winConfig = config as WindowsTextToSpeechSubsystemConfig;
                    if (winConfig != null)
                    {
                        winConfig.Voice = voice;

                        var msg = "Okay";

                        // Speak message
                        textToSpeechSubsystem.TrySpeak(msg, audioSource);
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot find a running TextToSpeechSubsystem. Please check the MRTK profile settings " +
                               "(Project Settings -> MRTK3) and/or ensure a TextToSpeechSubsystem is running.");
                OnSpeakFaulted?.Invoke("Cannot find a running TextToSpeechSubsystem.");
            }
        }

        public void SpeakFinished()
        {
            // If we have a text to speech manager on the target object, say something.
            // This voice will appear to emanate from the object.
            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
            if (textToSpeechSubsystem != null)
            {
                // profile associated with the Standalone build target group.
                MRTKProfile profile = MRTKProfile.Instance;

                if (profile == null)
                {
                    Debug.LogError("MRTK Profile could not be retrieved.");
                    return;
                }

                BaseSubsystemConfig config;

                // Attempt to retrieve the config associated with the indicated subsystem.
                if (!profile.TryGetConfigForSubsystem(typeof(WindowsTextToSpeechSubsystem), out config) || config == null)
                {
                    Debug.LogError($"Configuration could not be retrieved for {typeof(WindowsTextToSpeechSubsystem)}.", profile);
                }
                else
                {
                    WindowsTextToSpeechSubsystemConfig winConfig = config as WindowsTextToSpeechSubsystemConfig;
                    if (winConfig != null)
                    {
                        winConfig.Voice = voice;

                        var msg = "Bye";

                        // Speak message
                        textToSpeechSubsystem.TrySpeak(msg, audioSource);
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot find a running TextToSpeechSubsystem. Please check the MRTK profile settings " +
                               "(Project Settings -> MRTK3) and/or ensure a TextToSpeechSubsystem is running.");
                OnSpeakFaulted?.Invoke("Cannot find a running TextToSpeechSubsystem.");
            }
        }

        public void SpeakAnswer(string answer)
        {
            // If we have a text to speech manager on the target object, say something.
            // This voice will appear to emanate from the object.
            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
            if (textToSpeechSubsystem != null)
            {
                // profile associated with the Standalone build target group.
                MRTKProfile profile = MRTKProfile.Instance;

                if (profile == null)
                {
                    Debug.LogError("MRTK Profile could not be retrieved.");
                    return;
                }

                BaseSubsystemConfig config;

                // Attempt to retrieve the config associated with the indicated subsystem.
                if (!profile.TryGetConfigForSubsystem(typeof(WindowsTextToSpeechSubsystem), out config) || config == null)
                {
                    Debug.LogError($"Configuration could not be retrieved for {typeof(WindowsTextToSpeechSubsystem)}.", profile);
                }
                else
                {
                    WindowsTextToSpeechSubsystemConfig winConfig = config as WindowsTextToSpeechSubsystemConfig;
                    if (winConfig != null)
                    {
#if WINDOWS_UWP
                        SpeechSynthesizer synthesizer = new SpeechSynthesizer();

                        // find the desired voice
                        foreach (var v in SpeechSynthesizer.AllVoices)
                        {
                            if (v.DisplayName.Contains("Stefan"))
                            {
                                synthesizer.Voice = v;
                                Debug.Log("Selected Voice: " + v.DisplayName);
                                break;
                            }
                        }
#endif
#if WINDOWS_UWP
                        var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                        var voiceSec = synth.Voice;

                        Debug.Log("ACTIVE VOICE: " + voiceSec.DisplayName + " | " + voiceSec.Language);
#endif
                        winConfig.Voice = voice;

                        var msg = answer;

                        // Speak message
                        textToSpeechSubsystem.TrySpeak(msg, audioSource);
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot find a running TextToSpeechSubsystem. Please check the MRTK profile settings " +
                               "(Project Settings -> MRTK3) and/or ensure a TextToSpeechSubsystem is running.");
                OnSpeakFaulted?.Invoke("Cannot find a running TextToSpeechSubsystem.");
            }
        }

        public void SpeakNumber(string number)
        {
            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
            if (textToSpeechSubsystem != null)
            {
                MRTKProfile profile = MRTKProfile.Instance;
                if (profile == null) return;

                BaseSubsystemConfig config;
                if (!profile.TryGetConfigForSubsystem(typeof(WindowsTextToSpeechSubsystem), out config) || config == null) return;

                WindowsTextToSpeechSubsystemConfig winConfig = config as WindowsTextToSpeechSubsystemConfig;
                if (winConfig != null)
                {
                    winConfig.Voice = voice;
                    textToSpeechSubsystem.TrySpeak(number, audioSource);
                }
            }
        }

        public void SpeakHi()
        {
            // If we have a text to speech manager on the target object, say something.
            // This voice will appear to emanate from the object.
            textToSpeechSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<TextToSpeechSubsystem>();
            if (textToSpeechSubsystem != null)
            {
                // profile associated with the Standalone build target group.
                MRTKProfile profile = MRTKProfile.Instance;

                if (profile == null)
                {
                    Debug.LogError("MRTK Profile could not be retrieved.");
                    return;
                }

                BaseSubsystemConfig config;

                // Attempt to retrieve the config associated with the indicated subsystem.
                if (!profile.TryGetConfigForSubsystem(typeof(WindowsTextToSpeechSubsystem), out config) || config == null)
                {
                    Debug.LogError($"Configuration could not be retrieved for {typeof(WindowsTextToSpeechSubsystem)}.", profile);
                }
                else
                {
#if WINDOWS_UWP
                    var allVoices = SpeechSynthesizer.AllVoices;

                    Debug.Log("---- Available TTS Voices ----");

                    foreach (var v in allVoices)
                    {
                        Debug.Log($"Voice: {v.DisplayName} | Language: {v.Language} | Gender: {v.Gender} | Id: {v.Id}");
                    }

                    Debug.Log("---- End of Voice List ----");
#endif
                    WindowsTextToSpeechSubsystemConfig winConfig = config as WindowsTextToSpeechSubsystemConfig;
                    if (winConfig != null)
                    {
                        winConfig.Voice = voice;

                        var msg = string.Empty;

                        msg = string.Format(
                        "Hi",
                        winConfig.Voice.ToString());

                        // Speak message
                        textToSpeechSubsystem.TrySpeak(msg, audioSource);
                    }
                }
            }
            else
            {
                Debug.LogError("Cannot find a running TextToSpeechSubsystem. Please check the MRTK profile settings " +
                               "(Project Settings -> MRTK3) and/or ensure a TextToSpeechSubsystem is running.");
                OnSpeakFaulted?.Invoke("Cannot find a running TextToSpeechSubsystem.");
            }
        }
    }
}
