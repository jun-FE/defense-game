using System;
using System.Collections.Generic;
using System.IO;
using Akmong.Battle;
using UnityEngine;

/// <summary>
/// 칸 맵(GridMap) 파일 저장·불러오기. 파일은 사람이 읽을 수 있는 JSON이고, 칸은 줄마다 글자 하나씩이다
/// (. 암흑, o 길, # 막힌 암흑, c 작은 결정, C 큰 결정).
/// - 에디터(Unity)에서 저장: Assets/Resources/Maps/{ID}.json → Git에 같이 올라가고 게임에 포함된다.
/// - 빌드된 게임에서 저장: (사용자 데이터 폴더)/Maps/{ID}.json. 게임에 포함하려면 그 파일을 위 폴더로 옮긴다.
/// </summary>
public static class MapStorage
{
    public const string ResourcesFolder = "Maps";

    [Serializable]
    public class GridMapFile
    {
        public string id;
        public string name;
        public int width;
        public int height;
        public int spawnX = -1, spawnY = -1;
        public int goalX = -1, goalY = -1;
        public int smallCrystalValue = 10;
        public int largeCrystalValue = 40;
        [Tooltip(". 암흑  o 길  # 막힌 암흑  c 작은 결정  C 큰 결정")]
        public string[] rows = new string[0];
    }

    /// <summary>지금 저장하는 폴더(절대 경로).</summary>
    public static string SaveFolder => Application.isEditor
        ? Path.Combine(Application.dataPath, "Resources", ResourcesFolder)
        : Path.Combine(Application.persistentDataPath, ResourcesFolder);

    public static string ToJson(GridMap map)
    {
        var file = new GridMapFile
        {
            id = map.Id,
            name = map.Name,
            width = map.Width,
            height = map.Height,
            spawnX = map.SpawnX, spawnY = map.SpawnY,
            goalX = map.GoalX, goalY = map.GoalY,
            smallCrystalValue = map.SmallCrystalValue,
            largeCrystalValue = map.LargeCrystalValue,
            rows = map.ToRows(),
        };
        return JsonUtility.ToJson(file, true);
    }

    public static GridMap FromJson(string json)
    {
        var file = JsonUtility.FromJson<GridMapFile>(json);
        if (file == null) return null;
        GridMap map = GridMap.FromRows(file.rows);
        if (file.width > 0 && file.height > 0 && (file.width != map.Width || file.height != map.Height)) map.Resize(file.width, file.height);
        map.Id = string.IsNullOrEmpty(file.id) ? "MAP_NEW" : file.id;
        map.Name = file.name ?? "";
        map.SpawnX = file.spawnX; map.SpawnY = file.spawnY;
        map.GoalX = file.goalX; map.GoalY = file.goalY;
        map.SmallCrystalValue = file.smallCrystalValue;
        map.LargeCrystalValue = file.largeCrystalValue;
        if (!map.HasSpawn) map.SpawnX = map.SpawnY = -1;
        if (!map.HasGoal) map.GoalX = map.GoalY = -1;
        return map;
    }

    /// <summary>저장하고 저장한 파일 경로를 돌려준다. ID가 파일 이름이 된다.</summary>
    public static string Save(GridMap map)
    {
        Directory.CreateDirectory(SaveFolder);
        string path = Path.Combine(SaveFolder, SafeFileName(map.Id) + ".json");
        File.WriteAllText(path, ToJson(map));
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
        return path;
    }

    /// <summary>저장 폴더와 게임에 포함된 맵(Resources/Maps)의 ID 목록.</summary>
    public static List<string> ListIds()
    {
        var ids = new SortedSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(SaveFolder))
            foreach (string path in Directory.GetFiles(SaveFolder, "*.json")) ids.Add(Path.GetFileNameWithoutExtension(path));
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>(ResourcesFolder)) ids.Add(asset.name);
        return new List<string>(ids);
    }

    /// <summary>저장 폴더를 먼저 보고, 없으면 게임에 포함된 맵에서 찾는다. 없으면 null.</summary>
    public static GridMap Load(string id)
    {
        string path = Path.Combine(SaveFolder, SafeFileName(id) + ".json");
        if (File.Exists(path)) return FromJson(File.ReadAllText(path));
        var asset = Resources.Load<TextAsset>(ResourcesFolder + "/" + id);
        return asset != null ? FromJson(asset.text) : null;
    }

    public static string SafeFileName(string id)
    {
        if (string.IsNullOrEmpty(id)) return "MAP_NEW";
        foreach (char c in Path.GetInvalidFileNameChars()) id = id.Replace(c, '_');
        return id.Trim();
    }
}
