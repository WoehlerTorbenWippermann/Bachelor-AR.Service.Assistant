namespace Assets.Scripts.MyScripts.UiScripts
{
    using UnityEngine;

    /// <summary>
    /// Resets all dialogs (all components of type <see cref="ResettableDialog"/>)
    /// to their initial position and size.
    ///
    /// The public method <see cref="ResetAllDialogs"/> is intended to be triggered
    /// via a speech keyword ("reset") through a UnityEvent of the
    /// MySpeechKeywordRecognitionHandler.
    /// </summary>
    public class DialogResetHandler : MonoBehaviour
    {
        [Tooltip("Also reset deactivated (closed) dialogs.")]
        [SerializeField] private bool includeInactive = true;

        private void Start()
        {
            CaptureAllInitialStates();
        }

        /// <summary>
        /// Captures the initial pose of ALL dialogs once at scene start – including
        /// the closed (inactive) ones. Required because their own Awake only runs on
        /// the first open, so a reset before that would otherwise be skipped.
        /// </summary>
        private void CaptureAllInitialStates()
        {
            var dialogs = FindObjectsOfType<ResettableDialog>(true);
            foreach (var dialog in dialogs)
            {
                dialog.CaptureInitialIfNeeded();
            }

            Debug.Log($"[DialogReset] Captured initial state of {dialogs.Length} dialog(s).");
        }

        /// <summary>
        /// Resets all found dialogs to their initial state.
        /// </summary>
        public void ResetAllDialogs()
        {
            var dialogs = FindObjectsOfType<ResettableDialog>(includeInactive);

            foreach (var dialog in dialogs)
            {
                dialog.ResetToInitial();
            }

            Debug.Log($"[DialogReset] Reset {dialogs.Length} dialog(s) to initial position/size.");
        }
    }
}
