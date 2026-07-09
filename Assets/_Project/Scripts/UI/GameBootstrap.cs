using System;
using BadMovieClues.Core;
using BadMovieClues.Economy;
using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Per-gameplay-scene composition root: pulls app-lifetime services from
    /// the persistent AppRoot (constructed once, survives scene changes)
    /// instead of building its own, and constructs only the per-round
    /// GameController. Lives in UI (not Core) so Core never depends on UI.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameHud hud;

        private async Awaitable Start()
        {
            // Unity's Awaitable-returning lifecycle methods do not appear to
            // surface an exception thrown before the method's first await -
            // nothing ever observes the resulting faulted Awaitable, so it's
            // silently swallowed (no Console output at all). This try/catch
            // is a permanent safety net against exactly that.
            try
            {
                var app = AppRoot.Instance;
                var hintService = new HintService(app.Currency, app.Config);

                // Read and clear the daily challenge flag so it doesn't
                // persist across future navigations back to this scene.
                var isDaily = app.IsDailyChallenge;
                app.IsDailyChallenge = false;

                var controller = new GameController(
                    app.ContentProvider, app.Currency, hintService, app.Config, app.Progress,
                    isDailyChallenge: isDaily,
                    dailyRewardMultiplier: app.Config.DailyChallengeRewardMultiplier);

                // If this is a daily challenge, mark it completed when the player wins.
                if (isDaily)
                {
                    controller.Won += () => app.DailyChallenge.MarkCompleted();
                }

                hud.Bind(controller, app.AudioService);
                await controller.LoadLevelAsync(app.SelectedLevelIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameBootstrap] Exception in Start(): {e}");
            }
        }
    }
}
