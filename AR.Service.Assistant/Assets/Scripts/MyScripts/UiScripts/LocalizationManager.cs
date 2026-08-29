namespace Assets.Scripts.MyScripts.UiScripts
{
    using UnityEngine;
    using System;

    public enum AppLanguage { German, English }

    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public static event Action<AppLanguage> OnLanguageChanged;

        public AppLanguage CurrentLanguage { get; private set; } = AppLanguage.German;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetGerman()  => SetLanguage(AppLanguage.German);
        public void SetEnglish() => SetLanguage(AppLanguage.English);

        public void SetLanguage(AppLanguage language)
        {
            if (CurrentLanguage == language) return;
            CurrentLanguage = language;
            Debug.Log($"[LocalizationManager] Sprache gewechselt zu: {language}");
            OnLanguageChanged?.Invoke(language);
        }
    }
}
