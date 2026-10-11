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
    /// <summary>병정인형 400px 캔버스(인물 약 260px) → 약 1.1칸 높이.</summary>
    public const float SoldierPixelsPerUnit = 230f;
    /// <summary>스노우볼 384px 캔버스(구슬+받침 약 256px 높이) → 약 1.15칸 높이.</summary>
    public const float SnowballPixelsPerUnit = 225f;
    /// <summary>재봉틀 256px 캔버스(본체 약 205px 높이) → 약 1.15칸 높이.</summary>
    public const float SewingPixelsPerUnit = 180f;

    /// <summary>연출 조각(fx_*)은 그림 한 장 = 1칸 크기로 가져와 BattleFx가 배율로 크기를 정한다.</summary>
    static float FxPixelsPerUnit(string path)
    {
        if (path.EndsWith("fx_glow.png")) return 64f;
        if (path.EndsWith("fx_ring.png")) return 128f;
        if (path.EndsWith("fx_spark.png")) return 48f;
        if (path.EndsWith("fx_needle.png")) return 150f;
        return 0f;
    }

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder)) Apply((TextureImporter)assetImporter, assetPath);
    }

    /// <summary>설정이 바뀌었으면 true.</summary>
    public static bool Apply(TextureImporter importer, string path)
    {
        float fx = FxPixelsPerUnit(path);
        float ppu = fx > 0f ? fx
                  : path.Contains("projectile") ? ProjectilePixelsPerUnit
                  : path.Contains("/Soldier/soldier_") ? SoldierPixelsPerUnit
                  : path.Contains("/Snowball/snowball_") ? SnowballPixelsPerUnit
                  : path.Contains("/Sewing/sewing_") ? SewingPixelsPerUnit
                  : TowerPixelsPerUnit;
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
