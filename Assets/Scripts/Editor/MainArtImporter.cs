using UnityEditor;

/// <summary>Assets/Art/Main/Depth 아래 PNG를 메인 배경 레이어용 스프라이트로 가져온다(100 PPU = 1920×1080 화면 픽셀 기준).</summary>
public class MainArtImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Art/Main/";
    public const float PixelsPerUnit = 100f;

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder)) Apply((TextureImporter)assetImporter);
    }

    /// <summary>설정이 바뀌었으면 true.</summary>
    public static bool Apply(TextureImporter importer)
    {
        bool changed = importer.textureType != TextureImporterType.Sprite
                       || importer.spritePixelsPerUnit != PixelsPerUnit
                       || importer.mipmapEnabled
                       || importer.maxTextureSize < 2048;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        return changed;
    }
}
