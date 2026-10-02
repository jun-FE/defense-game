using UnityEditor;

/// <summary>Assets/Art/Main, Assets/Art/Lobby 아래 PNG를 화면 배경·UI용 스프라이트로 가져온다(100 PPU = 1920×1080 화면 픽셀 기준).</summary>
public class MainArtImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Art/Main/";
    public const string LobbyFolder = "Assets/Art/Lobby/";
    public const float PixelsPerUnit = 100f;

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder) || assetPath.StartsWith(LobbyFolder)) Apply((TextureImporter)assetImporter);
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
        // UI 조각은 작고 9-slice로 늘리므로 압축하지 않는다(가장자리 번짐 방지).
        importer.textureCompression = importer.assetPath.Contains("/UI/")
            ? TextureImporterCompression.Uncompressed
            : TextureImporterCompression.CompressedHQ;
        return changed;
    }
}
