namespace Assets.Scripts.MyScripts.UiScripts
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    public class ImageZoomOverlay : MonoBehaviour
    {
        [Tooltip("The RawImage in the overlay that shows the enlarged image.")]
        [SerializeField] private RawImage overlayImage;

        [Tooltip("How long the overlay stays visible (seconds).")]
        [SerializeField] private float displayDuration = 5f;

        private Coroutine _hideCoroutine;

        public void ShowOverlay(Texture texture)
        {
            if (overlayImage != null)
                overlayImage.texture = texture;

            gameObject.SetActive(true);

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        public void Dismiss()
        {
            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            gameObject.SetActive(false);
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            gameObject.SetActive(false);
            _hideCoroutine = null;
        }
    }
}
