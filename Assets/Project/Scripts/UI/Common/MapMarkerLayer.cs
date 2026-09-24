using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CGD.WorldMap;

namespace CGD.UI
{
    // Pooled icons for map markers under one RectTransform. Each frame: Begin, Place
    // every visible marker, End — unused icons are hidden, and icons are only ever
    // created when more markers are visible than ever before.
    public class MapMarkerLayer
    {
        private readonly RectTransform _root;
        private readonly List<Image>   _icons = new();
        private int _used;

        public MapMarkerLayer(RectTransform root) => _root = root;

        public void Begin() => _used = 0;

        public void Place(MapMarker marker, Vector2 localPosition, float rotation, float scale = 1f)
        {
            Image icon = Next();
            icon.sprite = SpriteFor(marker.Shape);
            icon.color  = marker.Color;

            RectTransform rt = icon.rectTransform;
            rt.anchoredPosition = localPosition;
            rt.sizeDelta        = Vector2.one * marker.Size * scale;
            rt.localRotation    = Quaternion.Euler(0f, 0f, rotation);
        }

        public void End()
        {
            for (int i = _used; i < _icons.Count; i++)
                if (_icons[i].enabled) _icons[i].enabled = false;
        }

        private Image Next()
        {
            if (_used == _icons.Count)
            {
                Image created = UIFactory.MakeImage("Marker", _root);
                RectTransform rt = created.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                _icons.Add(created);
            }

            Image icon = _icons[_used++];
            if (!icon.enabled) icon.enabled = true;
            return icon;
        }

        private static Sprite SpriteFor(MapMarkerShape shape) => shape switch
        {
            MapMarkerShape.Square  => ProceduralSprites.Square,
            MapMarkerShape.Diamond => ProceduralSprites.Diamond,
            MapMarkerShape.Arrow   => ProceduralSprites.Arrow,
            _                      => ProceduralSprites.Circle,
        };
    }
}
