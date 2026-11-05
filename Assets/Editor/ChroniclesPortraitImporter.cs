#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    // Portraits are Texture2D resources drawn by the portrait renderer, not UI atlases.
    public sealed class ChroniclesPortraitImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Chronicles/Portraits/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
#endif
