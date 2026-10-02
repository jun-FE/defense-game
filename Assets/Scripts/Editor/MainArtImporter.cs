using UnityEditor;

/// <summary>
/// Assets/Art/Main, Lobby, Battle, Maps 아래 PNG를 화면 배경·UI용 스프라이트로 가져온다(100 PPU = 1920×1080 화면 픽셀 기준).
/// 전투 맵 그림(Maps)은 크기와 상관없이 카메라 영역에 맞춰 늘려 깔리고, 4K 원본까지 받도록 최대 4096.
/// </summary>
public class MainArtImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Art/Main/";
    public const string LobbyFolder = "Assets/Art/Lobby/";
    public const string BattleFolder = "Assets/Art/Battle/";
    public const string MapsFolder = "Assets/Art/Maps/";
    public const float PixelsPerUnit = 100f;

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder) || assetPath.StartsWith(LobbyFolder) || assetPath.StartsWith(BattleFolder) || assetPath.StartsWith(MapsFolder))
            Apply((TextureImporter)assetImporter);
    }

    /// <summary>설정이 바뀌었으면 true.</summary>
    public static bool Apply(TextureImporter importer)
    {
        int maxSize = importer.assetPath.StartsWith(MapsFolder) ? 4096 : 2048;
        bool changed = importer.textureType != TextureImporterType.Sprite
                       || importer.spritePixelsPerUnit != PixelsPerUnit
                       || importer.mipmapEnabled
                       || importer.maxTextureSize < maxSize;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = maxSize;
        // UI 조각은 작고 9-slice로 늘리므로 압축하지 않는다(가장자리 번짐 방지).
        importer.textureCompression = importer.assetPath.Contains("/UI/")
            ? TextureImporterCompression.Uncompressed
            : TextureImporterCompression.CompressedHQ;
        return changed;
    }
}
