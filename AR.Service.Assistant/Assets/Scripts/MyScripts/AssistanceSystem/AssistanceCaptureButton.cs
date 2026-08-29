namespace Assets.Scripts.MyScripts.AssistanceSystem
{
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// Thin wrapper for the 3rd button in the hand dialog.
    /// Attach this script to the button GameObject.
    /// Finds the AssistanceManager automatically in the scene.
    /// </summary>
    public class AssistanceCaptureButton : MonoBehaviour
    {
        [Header("Optional Text")]
        [SerializeField] private TMP_Text buttonLabel;

        private AssistanceManager _manager;

        private void Start()
        {
            _manager = FindObjectOfType<AssistanceManager>();

            if (_manager == null)
                Debug.LogError("[AssistanceCaptureButton] AssistanceManager not found in the scene!");

            if (buttonLabel != null)
                buttonLabel.text = "Frage stellen";
        }

        /// <summary>Called by the button's OnClick event.</summary>
        public void OnButtonPressed()
        {
            _manager?.OnCaptureButtonPressed();
        }
    }
}
