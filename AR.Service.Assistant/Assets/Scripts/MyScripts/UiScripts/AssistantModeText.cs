namespace Assets.Scripts.MyScripts.UiScripts
{
    using Assets.Scripts.MyScripts.AssistanceSystem;
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(TMP_Text))]
    public class AssistantModeText : MonoBehaviour
    {
        private TMP_Text _label;

        private void Awake() { _label = GetComponent<TMP_Text>(); }

        private void OnEnable()
        {
            AssistanceManager.OnModeChanged += OnModeChanged;
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            UpdateLabel();
        }

        private void OnDisable()
        {
            AssistanceManager.OnModeChanged -= OnModeChanged;
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnModeChanged(AssistanceMode _) => UpdateLabel();
        private void OnLanguageChanged(AppLanguage _) => UpdateLabel();

        private void UpdateLabel()
        {
            if (_label == null) return;

            bool isAi = AssistanceManager.Instance != null
                && AssistanceManager.Instance.CurrentMode == AssistanceMode.AiAssistance;
            bool isEn = LocalizationManager.Instance != null
                && LocalizationManager.Instance.CurrentLanguage == AppLanguage.English;

            _label.text = isAi
                ? (isEn ? "AI Assistance" : "KI Assistenz")
                : (isEn ? "Human Assistance" : "Human Assistenz");
        }
    }
}
