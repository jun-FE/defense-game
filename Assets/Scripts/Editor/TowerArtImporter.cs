using UnityEditor;

/// <summary>
/// Assets/Art/Towers 아래 PNG를 자동으로 스프라이트로 가져온다.
/// 새 타워 그림도 이 폴더에 넣으면 같은 설정이 적용된다.
/// </summary>
public class TowerArtImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Art/Towers/";
    /// <summary>적 그림(256px 캔버스 = 1칸). EnemyAsset.scale로 크기를 정한다.</summary>
    public const string EnemyFolder = "Assets/Art/Enemies/";
    public const float TowerPixelsPerUnit = 150f;
    public const float ProjectilePixelsPerUnit = 220f;
    /// <summary>병정인형 400px 캔버스(인물 약 260px) → 약 1.1칸 높이.</summary>
    public const float SoldierPixelsPerUnit = 230f;
    /// <summary>스노우볼 384px 캔버스(구슬+받침 약 256px 높이) → 약 1.15칸 높이.</summary>
    public const float SnowballPixelsPerUnit = 225f;
    /// <summary>재봉틀 256px 캔버스(본체 약 205px 높이) → 약 1.15칸 높이.</summary>
    public const float SewingPixelsPerUnit = 180f;
    /// <summary>수선소 물품 타워(스탠드 조명·가위·줄자·바늘꽂이·수선표 활): 재봉틀과 같은 256px 캔버스 규칙.</summary>
    public static readonly string[] CraftTowers = { "/Lamp/lamp_", "/Scissors/scissors_", "/Tape/tape_", "/Pin/pin_", "/TagBow/tagbow_" };

    static bool IsCraftFrame(string path)
    {
        foreach (string prefix in CraftTowers)
            if (path.Contains(prefix)) return true;
        return false;
    }

    /// <summary>연출 조각(fx_*)은 그림 한 장 = 1칸 크기로 가져와 BattleFx가 배율로 크기를 정한다.</summary>
    static float FxPixelsPerUnit(string path)
    {
        if (path.EndsWith("fx_glow.png")) return 64f;
        if (path.EndsWith("fx_ring.png")) return 256f;
        if (path.EndsWith("fx_spark.png")) return 48f;
        if (path.EndsWith("fx_needle.png")) return 150f;
        if (path.EndsWith("fx_slash.png")) return 256f;
        return 0f;
    }

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder) || assetPath.StartsWith(EnemyFolder)) Apply((TextureImporter)assetImporter, assetPath);
    }

    /// <summary>설정이 바뀌었으면 true.</summary>
    public static bool Apply(TextureImporter importer, string path)
    {
        float fx = FxPixelsPerUnit(path);
        float ppu = path.StartsWith(EnemyFolder) ? 256f
                  : fx > 0f ? fx
                  : path.Contains("projectile") ? ProjectilePixelsPerUnit
                  : path.Contains("/Soldier/soldier_") ? SoldierPixelsPerUnit
                  : path.Contains("/Snowball/snowball_") ? SnowballPixelsPerUnit
                  : path.Contains("/Sewing/sewing_") || IsCraftFrame(path) ? SewingPixelsPerUnit
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
