using System;
using BadMovieClues.Economy;
using BadMovieClues.Services;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Coin-pack purchase panel: one button per IPurchaseService.Packs entry,
    /// a live coin balance, and a Restore Purchases action. Built
    /// procedurally by MainMenuScreen, same pattern as Settings/Level
    /// Select. This is the exact shell M15's real Play Billing integration
    /// plugs into - only StubPurchaseService changes, no call sites here.
    /// Sizing follows SettingsScreen's LayoutElement lesson (see that
    /// file's summary comment) since these buttons also live inside a
    /// ControlHeight-enabled VerticalLayoutGroup.
    /// </summary>
    public class StoreScreen : MonoBehaviour
    {
        private UITheme _theme;
        private AudioClip _clickSound;
        private IPurchaseService _purchases;
        private ICurrencyService _currency;
        private Action _onClose;
        private TextMeshProUGUI _balanceText;

        public void Init(UITheme theme, AudioClip clickSound, IPurchaseService purchases,
            ICurrencyService currency, Action onClose)
        {
            _theme = theme;
            _clickSound = clickSound;
            _purchases = purchases;
            _currency = currency;
            _onClose = onClose;
            Build();
        }

        public void Refresh() => _balanceText.text = $"Coins: {_currency.Balance}";

        private void Build()
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(transform, false);
            var panelRt = (RectTransform)panelGo.transform;
            panelRt.anchorMin = new Vector2(0.15f, 0.2f);
            panelRt.anchorMax = new Vector2(0.85f, 0.8f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _balanceText = MainMenuScreen.UIText(panelRt, "", 26, FontStyles.Bold);
            AddFixedHeight(_balanceText.gameObject, 44);
            if (_theme != null) _balanceText.color = _theme.NeutralLight;
            Refresh();

            foreach (var pack in _purchases.Packs)
            {
                var packId = pack.Id;
                BuildButton(panelRt, pack.Label, () => OnBuyClicked(packId));
            }

            BuildButton(panelRt, "Restore Purchases", OnRestoreClicked);
            BuildButton(panelRt, "< Back", () =>
            {
                PlayClick();
                _onClose();
            });
        }

        private static LayoutElement AddFixedHeight(GameObject go, float height)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return le;
        }

        private void BuildButton(Transform parent, string label, Action onClick)
        {
            var buttonGo = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            AddFixedHeight(buttonGo, 56);
            var button = buttonGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = MainMenuScreen.UIText(buttonGo.transform, label, 22, FontStyles.Normal);
            MainMenuScreen.StretchFull(text.rectTransform);

            button.onClick.AddListener(() =>
            {
                Tween.Scale(buttonGo.transform, endValue: 0.92f, duration: 0.08f, cycles: 2, cycleMode: CycleMode.Yoyo);
                onClick();
            });
        }

        private async void OnBuyClicked(string packId)
        {
            PlayClick();
            var success = await _purchases.PurchaseAsync(packId);
            if (success) Refresh();
        }

        private async void OnRestoreClicked()
        {
            PlayClick();
            await _purchases.RestorePurchasesAsync();
            Refresh();
        }

        private void PlayClick() => AppRoot.Instance.AudioService.PlayOneShot(_clickSound);
    }
}
