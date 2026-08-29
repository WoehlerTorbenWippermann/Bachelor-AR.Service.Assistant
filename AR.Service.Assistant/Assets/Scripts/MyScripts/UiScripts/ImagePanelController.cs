using UnityEngine;

namespace Assets.Scripts.MyScripts.UiScripts
{
    /// <summary>
    /// Controls the ImagePanel – responsible only for the close button.
    /// </summary>
    public class ImagePanelController : MonoBehaviour
    {
        [Tooltip("The root GameObject of the ImagePanel that is shown/hidden.")]
        [SerializeField] private GameObject panelRoot;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;
        }

        public void ShowPanel()
        {
            panelRoot.SetActive(true);
        }

        public void ClosePanel()
        {
            panelRoot.SetActive(false);
        }
    }
}
