using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Animates the button graphic downwards on PointerDown and restores it on PointerUp,
    /// simulating a tactile, mechanical 3D button press.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TactileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressDepth = 4f;
        [SerializeField] private float pressDuration = 0.05f;
        [SerializeField] private float releaseDuration = 0.1f;

        private Button _button;
        private RectTransform _target;
        private Vector3 _originalPos;
        private bool _isPressed;

        private void Awake()
        {
            _button = GetComponent<Button>();
            
            // Shift target graphic or first child
            if (_button.targetGraphic != null)
            {
                _target = _button.targetGraphic.rectTransform;
            }
            else if (transform.childCount > 0)
            {
                _target = transform.GetChild(0) as RectTransform;
            }
            else
            {
                _target = transform as RectTransform;
            }

            if (_target != null)
            {
                _originalPos = _target.anchoredPosition3D;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable || _target == null) return;
            _isPressed = true;
            Tween.StopAll(this);
            
            // Shift target face down
            Tween.UIAnchoredPosition3D(_target, endValue: _originalPos + new Vector3(0, -pressDepth, 0), duration: pressDuration, ease: Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed || _target == null) return;
            _isPressed = false;
            Tween.StopAll(this);
            
            // Bounce target face back up
            Tween.UIAnchoredPosition3D(_target, endValue: _originalPos, duration: releaseDuration, ease: Ease.OutBack);
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.anchoredPosition3D = _originalPos;
            }
            _isPressed = false;
        }
    }
}
