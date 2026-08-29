namespace Assets.Scripts.MyScripts.UiScripts
{
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(RawImage))]
    public class ImageZoomOnClick : MonoBehaviour
    {
        [Tooltip("The overlay panel shown on click.")]
        [SerializeField] private ImageZoomOverlay overlay;

        private RawImage _image;
        private BoxCollider _collider;

        private void Awake()
        {
            _image = GetComponent<RawImage>();

            _collider = GetComponent<BoxCollider>();
            if (_collider == null)
                _collider = gameObject.AddComponent<BoxCollider>();

            SyncColliderToRect();
        }

        private void OnEnable() => SyncCollider();

        public void SyncCollider() => SyncColliderToRect();

        private void SyncColliderToRect()
        {
            if (_collider == null || _image == null) return;
            var r = _image.rectTransform.rect;
            _collider.size = new Vector3(r.width, r.height, 0.01f);
            _collider.center = Vector3.zero;
        }

        public void OnClick()
        {
            if (overlay == null || _image.texture == null) return;
            overlay.ShowOverlay(_image.texture);
        }
    }
}
