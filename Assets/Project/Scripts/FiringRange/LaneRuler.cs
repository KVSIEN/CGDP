using UnityEngine;
using TMPro;

namespace CGD.FiringRange
{
    // Writes the distance from the firing line on the floor of the lane, along both walls, so
    // a shot's range can be read off at a glance. Stand it on the firing line, facing down
    // the lane; the labels are made when the scene starts.
    public class LaneRuler : MonoBehaviour
    {
        [Tooltip("Metres between marks")]
        [SerializeField, Min(1f)] private float _interval = 25f;
        [SerializeField, Min(1)]  private int   _marks    = 4;
        [Tooltip("How far each label sits from the lane's middle")]
        [SerializeField] private float _sideOffset = 6.5f;
        [SerializeField] private float _fontSize   = 6f;

        private void Awake()
        {
            for (int i = 1; i <= _marks; i++)
            {
                Label($"{i * _interval:0} m", -_sideOffset, i * _interval);
                Label($"{i * _interval:0} m", _sideOffset, i * _interval);
            }
        }

        // Lying flat, readable from the firing line.
        private void Label(string text, float x, float distance)
        {
            var label = new GameObject(text, typeof(TextMeshPro));
            label.transform.SetParent(transform, false);
            label.transform.SetLocalPositionAndRotation(new Vector3(x, 0.02f, distance), Quaternion.Euler(90f, 0f, 0f));

            var tmp = label.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = _fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.8f);
            tmp.rectTransform.sizeDelta = new Vector2(8f, 2f);
        }
    }
}
