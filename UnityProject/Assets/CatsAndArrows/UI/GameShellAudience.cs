using System;
using CatsAndArrows.Monetization;
using UnityEngine;
using UnityEngine.UIElements;

namespace CatsAndArrows.UI
{
    public sealed partial class GameShellView
    {
        private readonly IAudiencePreference audience;
        private readonly string privacyPolicyUrl, supportUrl;
        private readonly Action<string> openExternalUrl;
        public void OpenPrivacyPolicy() => OpenReleaseLink(privacyPolicyUrl);
        public void OpenSupport() => OpenReleaseLink(supportUrl);
        private void OpenReleaseLink(string url)
        {
            if (disposed || loading || CommercialBusy || Modal != null || Screen != "settings") return;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || parsed.Scheme != Uri.UriSchemeHttps) return;
            CancelInteraction(); ResetNotifications(); openExternalUrl(url);
        }
        private bool audienceFromMenu;
        public void ShowAudienceSelector(bool initial = false)
        {
            if (audience == null || disposed || loading || CommercialBusy || Modal != null ||
                (initial ? Screen != "menu" : Screen != "settings")) return;
            CancelInteraction(); CancelCompletion();
            audienceFromMenu = initial; Modal = "audience";
            overlay.Clear(); overlay.style.display = DisplayStyle.Flex;
            overlay.style.backgroundColor = new Color(0, 0, 0, .2f);
            overlay.style.alignItems = Align.Center; overlay.style.justifyContent = Justify.Center;
            overlay.style.paddingLeft = overlay.style.paddingRight = 12;
            overlay.style.paddingTop = overlay.style.paddingBottom = 12;
            var card = new VisualElement { name = "audience-selector" };
            card.style.width = Length.Percent(100); card.style.maxWidth = 320; card.style.maxHeight = Length.Percent(100);
            card.style.backgroundColor = PortraitBoardView.Background;
            card.style.paddingLeft = card.style.paddingRight = 16;
            card.style.paddingTop = card.style.paddingBottom = 12;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 14;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 14;
            overlay.Add(card);
            card.Add(Heading(text.GetText("audience.title"), 22));
            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "audience-scroll" };
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; scroll.style.flexShrink = 1;
            card.Add(scroll);
            var explanation = new Label(text.GetText("audience.explanation"));
            explanation.style.whiteSpace = WhiteSpace.Normal; explanation.style.fontSize = 16;
            explanation.style.marginBottom = 12; scroll.Add(explanation);
            AddAudienceBand(scroll, AudienceBand.Under13, "audience.under13");
            AddAudienceBand(scroll, AudienceBand.Teen, "audience.teen");
            AddAudienceBand(scroll, AudienceBand.Adult, "audience.adult");
            AddAudienceBand(scroll, AudienceBand.PreferNotToAnswer, "audience.private");
            card.Add(MakeButton("audience-back", "settings.back", CloseAudienceSelector));
            menu.SetEnabled(false); TickHintAttention();
            (card.Q<Button>("audience-" + audience.Current) ?? card.Q<Button>("audience-back")).Focus();
        }
        private void AddAudienceBand(VisualElement parent, AudienceBand band, string key)
        {
            var selected = band == audience.Current;
            var label = text.GetText(key) + (selected ? " ✓" : "");
            var button = new Button(() => SelectAudience(band)) { name = "audience-" + band, text = label, tooltip = label };
            StyleButton(button); button.style.minHeight = 56;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.borderLeftWidth = button.style.borderRightWidth = button.style.borderTopWidth = button.style.borderBottomWidth = selected ? 3 : 1;
            button.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            parent.Add(button);
        }
        public void SelectAudience(AudienceBand band)
        {
            if (audience == null || Modal != "audience" || CommercialBusy) return;
            CloseAudienceSelector(); // Native consent may begin only after the local selector closes.
            audience.Select(band);
        }
        private void CloseAudienceSelector()
        {
            if (audienceFromMenu) { ShowMenu(); menu.Q<Button>("menu-start")?.Focus(); }
            else { ShowSettings(); menu.Q<Button>("settings-age-range")?.Focus(); }
        }
        private void OnAudienceChanged()
        {
            if (disposed || loading) return;
            if (Screen == "play") BuildControls();
            if (Screen == "settings" && !CommercialBusy && Modal == null) ShowSettings();
        }
    }
}
