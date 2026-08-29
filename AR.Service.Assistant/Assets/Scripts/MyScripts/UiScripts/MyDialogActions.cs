namespace Assets.Scripts.MyScripts.UiScripts
{
    using Assets.Scripts.MyScripts.AssistanceSystem;
    using Assets.Scripts.MyScripts.SpeechHandler;
    using TMPro;
    using UnityEngine;

    public class MyDialogActions : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private MyDictationHandler dictationHandler;
        [SerializeField] private MyTextToSpeechHandler textToSpeechHandler;
        [SerializeField] private MySpeechKeywordRecognitionHandler speechKeywordRecognitionHandler;
        [SerializeField] private AssistanceManager assistanceManager;

        [Header("Dialog")]
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private TMP_Text dialogTitle;

        [Header("Dialog AI Assistant")]
        [SerializeField] private GameObject aiButton;
        [SerializeField] private TMP_Text aiButtonText;
        [SerializeField] private GameObject aiIconOn;
        [SerializeField] private GameObject aiIconOff;
        [SerializeField] private bool isAiEnabled = true;

        [Header("Button texts (empty = initial text from Paragraph)")]
        [Tooltip("Text in the button when AI mode is active. Leave empty = the initial text from the Paragraph is used.")]
        [SerializeField] private string aiModeText = "";
        [Tooltip("Text in the button when human mode is active.")]
        [SerializeField] private string humanModeText = "Zu KI wechseln";

        [Header("HandMenu AI Assistant")]
        [SerializeField] private GameObject handMenuAiButton;
        [SerializeField] private TMP_Text handMenuAiButtonText;
        [SerializeField] private GameObject handMenuAiIconOn;
        [SerializeField] private GameObject handMenuAiIconOff;

        private void Awake()
        {
            // Read the initial text from the Paragraph if aiModeText is not set
            if (string.IsNullOrEmpty(aiModeText) && aiButtonText != null)
                aiModeText = aiButtonText.text;

            if (assistanceManager == null)
                assistanceManager = FindObjectOfType<AssistanceManager>();

            // The mode is set EXCLUSIVELY via the editor toggle on the AssistanceManager.
            // This dialog script only mirrors the mode (for the text) instead of
            // overwriting it on start – that previously forced AI incorrectly.
            if (assistanceManager != null)
                isAiEnabled = assistanceManager.CurrentMode == AssistanceMode.AiAssistance;

            UpdateAiVisuals();

            speechKeywordRecognitionHandler?.EnableKeywordRecognition();
        }

        public void OnCloseButtonPressed()
        {
            if (dialogRoot != null)
                dialogRoot.SetActive(false);
        }

        public void OnAiButtonPressed()
        {
            isAiEnabled = !isAiEnabled;

            UpdateAiVisuals();

            // Inform the AssistanceManager about the new mode
            if (assistanceManager != null)
                assistanceManager.SetMode(isAiEnabled);
            else
                Debug.LogWarning("[MyDialogActions] AssistanceManager not found – mode not switched.");

        }

        private void UpdateAiVisuals()
        {
            string text = isAiEnabled ? aiModeText : humanModeText;

            // Button text
            if (aiButtonText != null)
                aiButtonText.text = text;

            // Toggle icons
            if (aiIconOn != null)
                aiIconOn.SetActive(isAiEnabled);
            if (aiIconOff != null)
                aiIconOff.SetActive(!isAiEnabled);

            // HandMenu button (synchronized)
            if (handMenuAiButtonText != null)
                handMenuAiButtonText.text = text;
            if (handMenuAiIconOn != null)
                handMenuAiIconOn.SetActive(isAiEnabled);
            if (handMenuAiIconOff != null)
                handMenuAiIconOff.SetActive(!isAiEnabled);
        }
    }
}
