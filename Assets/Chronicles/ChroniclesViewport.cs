using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Fits the original 540x960 portrait canvas inside Android/iOS safe areas.
    /// SafeArea uses bottom-left screen coordinates; IMGUI uses top-left.
    /// Preserves one uniform scale to avoid clipped or stretched hit targets.
    /// </summary>
    public static class ChroniclesViewport
    {
        public const float Width = 540f;
        public const float Height = 960f;

        public static Rect ContentRect(Rect safeArea, int screenWidth, int screenHeight)
        {
            Rect fallback = new Rect(0f, 0f,
                Mathf.Max(1, screenWidth), Mathf.Max(1, screenHeight));
            if (safeArea.width < 1f || safeArea.height < 1f)
                safeArea = fallback;
            Rect safe = Rect.MinMaxRect(
                Mathf.Clamp(safeArea.xMin, 0f, fallback.width),
                Mathf.Clamp(safeArea.yMin, 0f, fallback.height),
                Mathf.Clamp(safeArea.xMax, 0f, fallback.width),
                Mathf.Clamp(safeArea.yMax, 0f, fallback.height));
            if (safe.width < 1f || safe.height < 1f) safe = fallback;
            float scale = Mathf.Min(safe.width / Width, safe.height / Height);
            float x = safe.xMin + (safe.width - Width * scale) * .5f;
            float yFromTop = fallback.height - safe.yMax +
                (safe.height - Height * scale) * .5f;
            return new Rect(x, yFromTop, Width * scale, Height * scale);
        }

        public static Matrix4x4 Matrix(Rect safeArea, int screenWidth, int screenHeight,
            out float scale)
        {
            Rect region = ContentRect(safeArea, screenWidth, screenHeight);
            scale = region.width / Width;
            return Matrix4x4.TRS(new Vector3(region.x, region.y, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
        }
    }
}
