using UnityEngine;

[CreateAssetMenu(fileName = "CrosshairSettings", menuName = "CGD/Crosshair Settings")]
public class CrosshairSettings : ScriptableObject
{
    [Header("Lines")]
    public float lineLength = 20f;
    public float lineThickness = 2f;
    public float centerGap = 4f;

    [Header("Appearance")]
    [Range(0f, 1f)] public float opacity = 1f;
    public Color color = Color.white;

    [Header("Center Dot")]
    public bool showCenterDot;
    public float centerDotSize = 4f;

    [Header("Outline")]
    public bool showOutline;
    public Color outlineColor = Color.black;
    [Range(0f, 5f)] public float outlineThickness = 1f;
}
