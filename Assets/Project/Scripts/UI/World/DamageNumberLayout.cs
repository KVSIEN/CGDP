using UnityEngine;

namespace CGD.UI
{
    // Where each hit in a group sits, kept free of pooling and rendering so the shape a burst
    // builds can be reasoned about (and changed) on its own.
    public static class DamageNumberLayout
    {
        // Hits step out sideways in a fixed order — middle, right, left, further right, further
        // left — and lift slightly as the group deepens. Nothing here is random: the same burst
        // always builds the same shape, which is what makes a cluster readable at a glance
        // rather than merely tidy. Numbers are free to overlap; the pattern alone decides.
        public static Vector2 SlotOffset(int indexInCluster)
        {
            if (indexInCluster <= 0) return Vector2.zero;

            int   slot = indexInCluster % DamageNumberStyle.ColumnSlots;
            int   step = (slot + 1) / 2;
            float x    = (slot % 2 == 1 ? step : -step) * DamageNumberStyle.ColumnStepPx;
            float y    = Mathf.Min(indexInCluster, DamageNumberStyle.RowStepMax) * DamageNumberStyle.RowStepPx;

            return new Vector2(x, y);
        }
    }
}
