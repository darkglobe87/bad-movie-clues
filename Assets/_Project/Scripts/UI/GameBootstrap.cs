using BadMovieClues.Core;
using BadMovieClues.Data;
using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Composition root: constructs the content provider + GameController and
    /// wires them to the view. Lives in UI (not Core) so Core never depends
    /// on UI - UI is the one layer allowed to depend on everything else.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameHud hud;

        private async Awaitable Start()
        {
            IContentProvider contentProvider = new BundledContentProvider();
            var controller = new GameController(contentProvider);
            hud.Bind(controller);
            await controller.LoadLevelAsync(0);
        }
    }
}
