using System.Collections.Generic;

namespace Akmong.Battle
{
    /// <summary>
    /// 전투 시작 전 데이터 검사(시스템 기획서 30번). 오류 문장 목록을 돌려주며, 비어 있으면 통과.
    /// 중복 ID, 없는 참조, 범위를 벗어난 값을 잡는다.
    /// </summary>
    public static class DefinitionValidator
    {
        public static List<string> Validate(StageDef stage)
        {
            var errors = new List<string>();
            var ids = new Dictionary<string, HashSet<string>>();

            if (stage == null)
            {
                errors.Add("Stage가 없습니다.");
                return errors;
            }

            string where = $"Stage {Name(stage.Id)}";
            CheckId(ids, errors, "Stage", stage.Id, where);
            if (stage.StartCoin < 0) errors.Add($"{where}: start_coin은 0 이상이어야 합니다.");
            if (stage.CoreMaxHp < 1) errors.Add($"{where}: core_max_hp는 1 이상이어야 합니다.");
            if (stage.Waves.Count == 0) errors.Add($"{where}: 웨이브가 하나도 없습니다.");
            if (stage.Towers.Count == 0) errors.Add($"{where}: 지을 수 있는 타워가 없습니다.");

            if (stage.Map == null) errors.Add($"{where}: Map 참조가 비어 있습니다.");
            else ValidateMap(stage.Map, ids, errors);

            var enemies = new HashSet<EnemyDef>();
            for (int w = 0; w < stage.Waves.Count; w++)
            {
                WaveDef wave = stage.Waves[w];
                if (wave == null) { errors.Add($"{where}: {w}번 웨이브가 비어 있습니다."); continue; }
                string waveWhere = $"Wave {Name(wave.Id)}";
                CheckId(ids, errors, "Wave", wave.Id, waveWhere);
                if (wave.PrepareSec < 0) errors.Add($"{waveWhere}: prepare_sec는 0 이상이어야 합니다.");
                if (wave.ClearCoin < 0) errors.Add($"{waveWhere}: clear_coin은 0 이상이어야 합니다.");
                if (wave.Groups.Count == 0) errors.Add($"{waveWhere}: 생성 묶음이 하나도 없습니다.");

                foreach (SpawnGroupDef group in wave.Groups)
                {
                    string groupWhere = $"SpawnGroup {Name(group.Id)}";
                    CheckId(ids, errors, "SpawnGroup", group.Id, groupWhere);
                    if (stage.Map != null && stage.Map.FindSpawn(group.SpawnId) == null)
                        errors.Add($"{groupWhere}: 출현 지점 '{group.SpawnId}'가 맵에 없습니다.");
                    if (group.Enemy == null) errors.Add($"{groupWhere}: Enemy 참조가 비어 있습니다.");
                    else enemies.Add(group.Enemy);
                    if (group.Count < 1) errors.Add($"{groupWhere}: count는 1 이상이어야 합니다.");
                    if (group.StartSec < 0) errors.Add($"{groupWhere}: start_sec는 0 이상이어야 합니다.");
                    if (group.IntervalSec <= 0) errors.Add($"{groupWhere}: interval_sec는 0보다 커야 합니다.");
                }
            }

            foreach (EnemyDef enemy in enemies) ValidateEnemy(enemy, ids, errors);
            foreach (TowerDef tower in stage.Towers)
            {
                if (tower == null) errors.Add($"{where}: 타워 목록에 빈 항목이 있습니다.");
                else ValidateTower(tower, ids, errors);
            }
            return errors;
        }

        static void ValidateMap(MapDef map, Dictionary<string, HashSet<string>> ids, List<string> errors)
        {
            string where = $"Map {Name(map.Id)}";
            CheckId(ids, errors, "Map", map.Id, where);
            if (map.SpawnPoints.Count == 0) errors.Add($"{where}: 출현 지점이 없습니다.");
            if (map.BuildZones.Count == 0) errors.Add($"{where}: 건설 구역이 없습니다.");
            if (map.CameraWidth <= 0 || map.CameraHeight <= 0) errors.Add($"{where}: camera_bounds의 폭과 높이는 양수여야 합니다.");
            foreach (SpawnPointDef spawn in map.SpawnPoints)
            {
                string spawnWhere = $"SpawnPoint {Name(spawn.Id)}";
                CheckId(ids, errors, "SpawnPoint", spawn.Id, spawnWhere);
                if (spawn.Path.Count < 2) errors.Add($"{spawnWhere}: path_points는 최소 2개여야 합니다.");
                else if (spawn.Length <= 0) errors.Add($"{spawnWhere}: 경로 길이가 0입니다.");
            }
        }

        static void ValidateEnemy(EnemyDef enemy, Dictionary<string, HashSet<string>> ids, List<string> errors)
        {
            string where = $"Enemy {Name(enemy.Id)}";
            CheckId(ids, errors, "Enemy", enemy.Id, where);
            if (enemy.MaxHp < 1) errors.Add($"{where}: max_hp는 1 이상이어야 합니다.");
            if (enemy.MoveSpeed <= 0) errors.Add($"{where}: move_speed는 0보다 커야 합니다.");
            if (enemy.CoreDamage < 0) errors.Add($"{where}: core_damage는 0 이상이어야 합니다.");
            if (enemy.KillCoin < 0) errors.Add($"{where}: kill_coin은 0 이상이어야 합니다.");
            if (enemy.Armor < 0) errors.Add($"{where}: armor는 0 이상이어야 합니다.");
        }

        static void ValidateTower(TowerDef tower, Dictionary<string, HashSet<string>> ids, List<string> errors)
        {
            string where = $"Tower {Name(tower.Id)}";
            CheckId(ids, errors, "Tower", tower.Id, where);
            if (tower.BuildCost < 0) errors.Add($"{where}: build_cost는 0 이상이어야 합니다.");
            if (tower.Levels.Count == 0) errors.Add($"{where}: 단계(TowerLevel)가 없습니다.");
            for (int i = 0; i < tower.Levels.Count; i++)
            {
                TowerLevelDef level = tower.Levels[i];
                string levelWhere = $"TowerLevel {Name(level.Id)}";
                CheckId(ids, errors, "TowerLevel", level.Id, levelWhere);
                if (level.Damage < 0) errors.Add($"{levelWhere}: damage는 0 이상이어야 합니다.");
                if (level.Range <= 0) errors.Add($"{levelWhere}: range는 0보다 커야 합니다.");
                if (level.AttackSec <= 0) errors.Add($"{levelWhere}: attack_sec는 0보다 커야 합니다.");
                if (level.CritChance < 0 || level.CritChance > 1) errors.Add($"{levelWhere}: crit_chance는 0~1이어야 합니다.");
                if (level.CritMult < 1) errors.Add($"{levelWhere}: crit_mult는 1 이상이어야 합니다.");
                if (level.UpgradeCost < 0) errors.Add($"{levelWhere}: upgrade_cost는 0 이상이어야 합니다.");
                if (level.BlockCount < 0) errors.Add($"{levelWhere}: block_count는 0 이상이어야 합니다.");
                if (level.BlockCount > 0 && level.BlockSec <= 0) errors.Add($"{levelWhere}: 저지하는 타워는 block_sec가 0보다 커야 합니다.");
                if (level.Knockback < 0) errors.Add($"{levelWhere}: knockback은 0 이상이어야 합니다.");
            }
        }

        static void CheckId(Dictionary<string, HashSet<string>> ids, List<string> errors, string table, string id, string where)
        {
            if (string.IsNullOrEmpty(id))
            {
                errors.Add($"{where}: ID가 비어 있습니다.");
                return;
            }
            HashSet<string> seen;
            if (!ids.TryGetValue(table, out seen)) ids[table] = seen = new HashSet<string>();
            if (!seen.Add(id)) errors.Add($"{table} ID '{id}'가 중복됩니다.");
        }

        static string Name(string id) => string.IsNullOrEmpty(id) ? "(ID 없음)" : id;
    }
}
