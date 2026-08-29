namespace Assets.Scripts.MyScripts.AssistanceSystem
{
    using Assets.Scripts.MyScripts.UiScripts;
    using TMPro;
    using UnityEngine;

    public class AssistanceModeToggle : MonoBehaviour
    {
        [Tooltip("TMP text of the button – resolved automatically from child objects when empty.")]
        [SerializeField] private TMP_Text buttonLabel;

        private void Start()
        {
            if (buttonLabel == null)
                buttonLabel = GetComponentInChildren<TMP_Text>();
            UpdateLabel();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            AssistanceManager.OnModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
            AssistanceManager.OnModeChanged -= OnModeChanged;
        }

        private void OnLanguageChanged(AppLanguage _) => UpdateLabel();
        private void OnModeChanged(AssistanceMode _) => UpdateLabel();

        public void ToggleMode()
        {
            if (AssistanceManager.Instance == null) return;

            if (AssistanceManager.Instance.CurrentMode == AssistanceMode.AiAssistance)
                AssistanceManager.Instance.SetHumanMode();
            else
                AssistanceManager.Instance.SetAiMode();
        }

        private void UpdateLabel()
        {
            if (buttonLabel == null) return;
            if (AssistanceManager.Instance == null) return;

            bool isAi = AssistanceManager.Instance.CurrentMode == AssistanceMode.AiAssistance;
            bool isEn = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == AppLanguage.English;

            string text = isAi
                ? (isEn ? "Switch to Human Assistance" : "Wechsel zur Human Assistance")
                : (isEn ? "Switch to AI Assistance"    : "Wechsel zur KI Assistance");

            Debug.Log($"[ModeToggle:{gameObject.name}] Mode={AssistanceManager.Instance.CurrentMode} isEn={isEn} → \"{text}\"");
            buttonLabel.text = text;
        }
    }
}
