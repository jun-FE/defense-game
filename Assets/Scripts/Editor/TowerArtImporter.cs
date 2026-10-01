using UnityEditor;

/// <summary>
/// Assets/Art/Towers 아래 PNG를 자동으로 스프라이트로 가져온다.
/// 새 타워 그림도 이 폴더에 넣으면 같은 설정이 적용된다.
/// </summary>
public class TowerArtImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Art/Towers/";
    public const float TowerPixelsPerUnit = 150f;
    public const float ProjectilePixelsPerUnit = 220f;

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder)) Apply((TextureImporter)assetImporter, assetPath);
    }

    /// <summary>설정이 바뀌었으면 true.</summary>
    public static bool Apply(TextureImporter importer, string path)
    {
        float ppu = path.Contains("projectile") ? ProjectilePixelsPerUnit : TowerPixelsPerUnit;
        bool changed = importer.textureType != TextureImporterType.Sprite
                       || importer.spriteImportMode != SpriteImportMode.Single
                       || importer.spritePixelsPerUnit != ppu
                       || importer.mipmapEnabled
                       || importer.textureCompression != TextureImporterCompression.Uncompressed;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        return changed;
    }
}
