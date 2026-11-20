using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Combat;
using CGD.UI;

namespace CGD.DevTools
{
    // The squad sizes (Squads, G): a row per faction with - and + buttons for how many of its
    // agents are in the fight. Changes apply at once and the list stays open, so several can be
    // made in a row. Built on the spectator HUD's canvas, like the scenario list.
    internal sealed class SquadMenu
    {
        private const float Width     = 440f;
        private const float RowHeight = 52f;
        private const float Header    = 56f;
        private const float Footer    = 20f;

        private readonly SquadSizer        _sizer;
        private readonly Action<string>    _toast;
        private readonly GameObject        _root;
        private readonly TextMeshProUGUI[] _counts;

        public SquadMenu(RectTransform canvas, SquadSizer sizer, string closeKey, Action<string> toast)
        {
            _sizer  = sizer;
            _toast  = toast;
            _counts = new TextMeshProUGUI[sizer.Teams.Count];

            RectTransform rect = SpectatorWindow.Centred("Squad Sizes", canvas, new Vector2(Width, Header + Mathf.Max(1, _counts.Length) * RowHeight + Footer));
            _root = rect.gameObject;

            SpectatorWindow.Label(rect, "Title", "Squad sizes", 22f, new Vector2(20f, -14f), new Vector2(200f, 30f));
            TextMeshProUGUI hint = SpectatorWindow.Label(rect, "Hint", $"click - / +  ·  {closeKey} closes", 13f,
                                                         new Vector2(Width - 220f, -18f), new Vector2(200f, 24f));
            hint.alignment = TextAlignmentOptions.MidlineRight;
            hint.color = SpectatorWindow.MutedText;

            for (int i = 0; i < _counts.Length; i++)
                BuildRow(rect, i, sizer.Teams[i]);
            _root.SetActive(false);
        }

        public bool IsOpen => _root.activeSelf;

        public void Toggle()
        {
            _root.SetActive(!IsOpen);
            if (IsOpen) Refresh();
        }

        public void Close() => _root.SetActive(false);

        private void BuildRow(RectTransform rect, int index, Team team)
        {
            float y = -Header - index * RowHeight;

            TextMeshProUGUI name = SpectatorWindow.Label(rect, "Faction " + team, team.ToString(), 20f, new Vector2(20f, y), new Vector2(160f, 36f));
            name.color = _sizer.ColorOf(team);

            Button minus = UIFactory.MakeButton("Fewer " + team, rect, "-", out TextMeshProUGUI minusLabel);
            minusLabel.fontSize = 22f;
            UIFactory.Place(minus.GetComponent<RectTransform>(), new Vector2(200f, y), new Vector2(40f, 36f));
            minus.onClick.AddListener(() => Change(team, -1));

            TextMeshProUGUI count = SpectatorWindow.Label(rect, "Count " + team, "", 20f, new Vector2(246f, y), new Vector2(90f, 36f));
            count.alignment = TextAlignmentOptions.Center;
            _counts[index] = count;

            Button plus = UIFactory.MakeButton("More " + team, rect, "+", out TextMeshProUGUI plusLabel);
            plusLabel.fontSize = 22f;
            UIFactory.Place(plus.GetComponent<RectTransform>(), new Vector2(342f, y), new Vector2(40f, 36f));
            plus.onClick.AddListener(() => Change(team, +1));
        }

        private void Change(Team team, int delta)
        {
            int before = _sizer.Active(team);
            _sizer.Resize(team, before + delta);
            if (_sizer.Active(team) == before)
                _toast(delta > 0 ? $"All {_sizer.Capacity(team)} {team} agents are already in" : $"No {team} agents left to remove");
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < _counts.Length; i++)
            {
                Team team = _sizer.Teams[i];
                _counts[i].text = $"{_sizer.Active(team)} / {_sizer.Capacity(team)}";
            }
        }
    }
}
