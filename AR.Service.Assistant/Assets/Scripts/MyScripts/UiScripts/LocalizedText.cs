namespace Assets.Scripts.MyScripts.UiScripts
{
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string textDe;
        [SerializeField] private string textEn;

        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += Apply;
            ApplyCurrent();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= Apply;
        }

        private void Apply(AppLanguage language)
        {
            if (_label == null) _label = GetComponent<TMP_Text>();
            _label.text = language == AppLanguage.English ? textEn : textDe;
        }

        private void ApplyCurrent()
        {
            if (LocalizationManager.Instance != null)
                Apply(LocalizationManager.Instance.CurrentLanguage);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Live preview in the Editor while typing
            if (_label == null) _label = GetComponent<TMP_Text>();
            if (_label != null && !string.IsNullOrEmpty(textDe))
                _label.text = textDe;
        }
#endif
    }
}
