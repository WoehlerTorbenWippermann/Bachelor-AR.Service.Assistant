namespace Assets.Scripts.MyScripts.UiScripts
{
    using UnityEngine;

    /// <summary>
    /// Controls the configuration dialog.
    /// - Always hidden on start.
    /// - OpenDialog() / CloseDialog() are called via keyword or the close button.
    /// </summary>
    public class ConfigDialogController : MonoBehaviour
    {
        [SerializeField] private GameObject dialogRoot;

        private void Awake()
        {
            if (dialogRoot == null)
                dialogRoot = gameObject;

            dialogRoot.SetActive(false);
        }

        public void OpenDialog()
        {
            dialogRoot.SetActive(true);
        }

        public void CloseDialog()
        {
            dialogRoot.SetActive(false);
        }
    }
}
