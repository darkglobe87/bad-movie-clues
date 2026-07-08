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
    /// Coin-pack purchase panel: one card pack row per CoinPack entry,
    /// a live gold-themed coin balance, and a Restore Purchases action.
    /// Built procedurally by MainMenuScreen with themed cards and native vibrations.
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

        public void Refresh()
        {
            if (_balanceText != null)
            {
                _balanceText.text = $"● Balance: {_currency.Balance}";
                if (_theme != null) _balanceText.color = _theme.CoinTextColor;
            }
        }

        private void Build()
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(transform, false);
            var panelRt = (RectTransform)panelGo.transform;
            panelRt.anchorMin = new Vector2(0.1f, 0.15f);
            panelRt.anchorMax = new Vector2(0.9f, 0.85f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;

            var panelImage = panelGo.GetComponent<Image>();
            if (_theme != null)
            {
                _theme.ApplyPanel(panelImage);
            }
            else
            {
                panelImage.color = new Color32(0x35, 0x20, 0x4E, 0xFF);
            }

            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _balanceText = MainMenuScreen.UIText(panelRt, "", 26, FontStyles.Bold);
            if (_theme != null && _theme.HeadingFont != null) _balanceText.font = _theme.HeadingFont;
            AddFixedHeight(_balanceText.gameObject, 44);
            Refresh();

            if (_theme != null)
            {
                _theme.CreateSeparator(panelRt, 2f);
            }

            foreach (var pack in _purchases.Packs)
            {
                BuildPackRow(panelRt, pack);
            }

            if (_theme != null)
            {
                _theme.CreateSeparator(panelRt, 2f);
            }

            BuildButton(panelRt, "Restore Purchases", OnRestoreClicked);
            BuildButton(panelRt, "< Back", () =>
            {
                PlayClick();
                _onClose();
            });
        }

        private void BuildPackRow(Transform parent, CoinPack pack)
        {
            var rowGo = new GameObject($"Pack_{pack.Id}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);
            AddFixedHeight(rowGo, 64);
            
            var rowImage = rowGo.GetComponent<Image>();
            if (_theme != null)
            {
                _theme.ApplyCard(rowImage, isInteractive: true);
            }
            else
            {
                rowImage.color = new Color32(0x23, 0x14, 0x34, 0xFF);
            }

            var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            // Coin Label
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(rowGo.transform, false);
            labelGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            string labelText = $"● {pack.Coins} Coins";
            if (pack.Id == "coins_large")
            {
                string limeHex = _theme != null ? ColorUtility.ToHtmlStringRGB(_theme.AccentLime) : "B6FF3C";
                labelText += $" <color=#{limeHex}><size=14>BEST VALUE</size></color>";
            }

            var text = MainMenuScreen.UIText(labelGo.transform, labelText, 20, FontStyles.Bold);
            if (_theme != null && _theme.HeadingFont != null) text.font = _theme.HeadingFont;
            MainMenuScreen.StretchFull(text.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if (_theme != null) text.color = _theme.CoinTextColor;

            // Buy Button
            var buttonGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(rowGo.transform, false);
            var le = buttonGo.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            le.minWidth = 100;

            var button = buttonGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var buyText = MainMenuScreen.UIText(buttonGo.transform, "Buy", 18, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) buyText.font = _theme.BodyFont;
            MainMenuScreen.StretchFull(buyText.rectTransform);

            var packId = pack.Id;
            button.onClick.AddListener(() =>
            {
                Tween.Scale(buttonGo.transform, endValue: 0.92f, duration: 0.08f, cycles: 2, cycleMode: CycleMode.Yoyo);
                OnBuyClicked(packId);
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
            AddFixedHeight(buttonGo, 48);
            var button = buttonGo.GetComponent<Button>();
            if (_theme != null) _theme.ApplyButton(button, buttonGo.GetComponent<Image>());

            var text = MainMenuScreen.UIText(buttonGo.transform, label, 20, FontStyles.Normal);
            if (_theme != null && _theme.BodyFont != null) text.font = _theme.BodyFont;
            MainMenuScreen.StretchFull(text.rectTransform);

            button.onClick.AddListener(() =>
            {
                PlayClick();
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

        private void PlayClick()
        {
            AppRoot.Instance.Haptics?.VibrateClick();
            AppRoot.Instance.AudioService.PlayOneShot(_clickSound);
        }
    }
}
