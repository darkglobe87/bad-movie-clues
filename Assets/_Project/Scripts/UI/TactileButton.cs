using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PrimeTween;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Animates the button graphic by scaling it down on PointerDown and restoring it on PointerUp,
    /// simulating a tactile 3D button press without fighting LayoutGroups.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TactileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float squishScale = 0.92f;
        [SerializeField] private float pressDuration = 0.05f;
        [SerializeField] private float releaseDuration = 0.1f;

        private Button _button;
        private RectTransform _target;
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
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable || _target == null) return;
            _isPressed = true;
            Tween.StopAll(this);
            
            // Squish down
            Tween.Scale(_target, endValue: squishScale, duration: pressDuration, ease: Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed || _target == null) return;
            _isPressed = false;
            Tween.StopAll(this);
            
            // Bounce back
            Tween.Scale(_target, endValue: 1f, duration: releaseDuration, ease: Ease.OutBack);
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.localScale = Vector3.one;
            }
            _isPressed = false;
        }
    }
}
