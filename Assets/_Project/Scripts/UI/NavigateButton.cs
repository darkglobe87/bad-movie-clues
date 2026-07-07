using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>Tiny reusable button that navigates to a named scene via ScreenNavigator.</summary>
    [RequireComponent(typeof(Button))]
    public class NavigateButton : MonoBehaviour
    {
        [SerializeField] private string targetScene;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => _ = ScreenNavigator.Instance.LoadScene(targetScene));
        }
    }
}
