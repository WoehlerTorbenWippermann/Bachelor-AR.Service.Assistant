namespace Assets.Scripts.MyScripts.UiScripts
{
    using UnityEngine;

    /// <summary>
    /// Attach to the root transform of each dialog.
    ///
    /// On first activation it remembers the initial position, rotation and size
    /// (scale) and can restore them via <see cref="ResetToInitial"/> – e.g. when a
    /// dialog was unintentionally moved or distorted through grabbing/scaling.
    ///
    /// Only the pose is reset – the visibility (active/inactive) stays untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public class ResettableDialog : MonoBehaviour
    {
        [Tooltip("Optional: transform whose pose is reset. Empty = this GameObject.")]
        [SerializeField] private Transform target;

        // Remembered initial values (local, relative to the parent)
        private bool _captured;
        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;
        private Vector3 _initialLocalScale;

        // Additionally for canvas-/RectTransform-based dialogs
        private RectTransform _rectTransform;
        private Vector2 _initialSizeDelta;
        private Vector3 _initialAnchoredPosition;

        /// <summary>
        /// True as soon as an initial state has been captured.
        /// </summary>
        public bool IsCaptured => _captured;

        private void Awake()
        {
            if (target == null)
                target = transform;

            CaptureInitialIfNeeded();
        }

        /// <summary>
        /// Remembers the pose only if no initial state exists yet.
        /// Important for dialogs that are closed (inactive) at start: their Awake
        /// only runs on the first open, so the DialogResetHandler pre-captures them
        /// at scene start via this method.
        /// </summary>
        public void CaptureInitialIfNeeded()
        {
            if (_captured)
                return;

            CaptureInitial();
        }

        /// <summary>
        /// Remembers the current pose as the initial state (overwrites an already
        /// remembered state). Call this for a new reference state.
        /// </summary>
        public void CaptureInitial()
        {
            if (target == null)
                target = transform;

            _initialLocalPosition = target.localPosition;
            _initialLocalRotation = target.localRotation;
            _initialLocalScale = target.localScale;

            _rectTransform = target as RectTransform;
            if (_rectTransform != null)
            {
                _initialSizeDelta = _rectTransform.sizeDelta;
                _initialAnchoredPosition = _rectTransform.anchoredPosition3D;
            }

            _captured = true;
        }

        /// <summary>
        /// Resets position, rotation and size to the remembered initial state.
        /// </summary>
        public void ResetToInitial()
        {
            if (!_captured)
            {
                Debug.LogWarning($"[ResettableDialog] '{name}': No initial state captured – reset skipped.");
                return;
            }

            target.localPosition = _initialLocalPosition;
            target.localRotation = _initialLocalRotation;
            target.localScale = _initialLocalScale;

            if (_rectTransform != null)
            {
                _rectTransform.sizeDelta = _initialSizeDelta;
                _rectTransform.anchoredPosition3D = _initialAnchoredPosition;
            }
        }
    }
}
