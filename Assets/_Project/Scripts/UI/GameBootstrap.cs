using System;
using BadMovieClues.Core;
using BadMovieClues.Data;
using BadMovieClues.Economy;
using BadMovieClues.Services;
using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Composition root: constructs the content provider, economy services,
    /// and GameController, and wires them to the view. Lives in UI (not
    /// Core) so Core never depends on UI - UI is the one layer allowed to
    /// depend on everything else.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameHud hud;
        [SerializeField] private GameConfig config;

        private async Awaitable Start()
        {
            // Unity's Awaitable-returning lifecycle methods do not appear to
            // surface an exception thrown before the method's first await -
            // nothing ever observes the resulting faulted Awaitable, so it's
            // silently swallowed (no Console output at all). This try/catch
            // is a permanent safety net against exactly that.
            try
            {
                IContentProvider contentProvider = new BundledContentProvider();
                ISaveService saveService = new LocalJsonSaveService();
                ICurrencyService currency = new CurrencyService(saveService, config.StartingBalance);
                var hintService = new HintService(currency, config);

                var controller = new GameController(contentProvider, currency, hintService, config);
                hud.Bind(controller);
                await controller.LoadLevelAsync(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameBootstrap] Exception in Start(): {e}");
            }
        }
    }
}
