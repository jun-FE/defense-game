using System;
using System.Collections.Generic;
using System.Numerics;

namespace Akmong.Battle
{
    // 전투가 읽는 고정 데이터(시스템 기획서 7장). 전투 중에는 절대 수정하지 않는다.
    // Unity의 ScriptableObject 에셋에서 변환해 만들고, 테스트에서는 코드로 직접 만든다.

    /// <summary>GameRule: 게임 전반 정책.</summary>
    public sealed class GameRules
    {
        public string Id = "RULE_BASE";
        public float ArmorConstant = 100f;
        public float MinAttackSec = 0.20f;
        public float FixedDt = 0.05f;
        public float MinMoveRatio = 0.25f;
        public float MaxMoveRatio = 2.0f;
        public float MinDamagePct = -0.9f;
    }

    public sealed class EnemyDef
    {
        public string Id;
        public string Name;
        public int MaxHp = 100;
        public float Armor;
        public float MoveSpeed = 1.5f;
        public int CoreDamage = 5;
        public int KillCoin = 3;
        /// <summary>보스: 밀어내기 면역(타워 역할 기획).</summary>
        public bool IsBoss;
    }

    public enum TargetRule { NearestCore }

    public sealed class TowerLevelDef
    {
        public string Id;
        public float Damage;
        public float Range = 4f;
        public float AttackSec = 1f;
        public float CritChance;
        public float CritMult = 1.5f;
        /// <summary>다음 단계로 강화하는 비용. 마지막 단계는 0.</summary>
        public int UpgradeCost;

        // 근거리 저지(병정인형). 타워 역할 기획서 기준이며 수치는 밸런싱에서 확정한다.
        /// <summary>동시에 붙잡아 둘 수 있는 적 수. 0이면 저지하지 않는 타워.</summary>
        public int BlockCount;
        /// <summary>한 적을 붙잡아 두는 최대 시간(초). 지나면 그 적은 이 타워를 뚫고 지나간다.</summary>
        public float BlockSec;
        /// <summary>공격할 때 적을 경로 뒤로 미는 거리(타일). 보스는 면역.</summary>
        public float Knockback;

        // 감속(스노우볼). 맞은 적의 이동 속도에 곱한다. 1이면 감속 없음.
        /// <summary>맞은 적의 이동 속도 배율(0.7이면 30% 느려짐). 1이면 감속 없음. 보스는 효과가 절반.</summary>
        public float SlowMult = 1f;
        /// <summary>감속이 이어지는 시간(초). 다시 맞으면 새로 이어진다.</summary>
        public float SlowSec;
        public bool Slows => SlowMult < 1f && SlowSec > 0f;
    }

    public sealed class TowerDef
    {
        public string Id;
        public string Name;
        public int BuildCost;
        public TargetRule TargetRule = TargetRule.NearestCore;
        public List<TowerLevelDef> Levels = new List<TowerLevelDef>();
    }

    /// <summary>출현 지점과 고정 경로. 마지막 점이 꿈의 중심 접근 위치.</summary>
    public sealed class SpawnPointDef
    {
        public string Id;
        public List<Vector2> Path = new List<Vector2>();

        float[] cumulative;

        public float Length
        {
            get { EnsureCache(); return cumulative.Length == 0 ? 0f : cumulative[cumulative.Length - 1]; }
        }

        /// <summary>경로 시작점에서 distance만큼 간 위치.</summary>
        public Vector2 PointAt(float distance)
        {
            EnsureCache();
            if (Path.Count == 0) return Vector2.Zero;
            if (distance <= 0f) return Path[0];
            for (int i = 1; i < Path.Count; i++)
            {
                if (distance <= cumulative[i])
                {
                    float segment = cumulative[i] - cumulative[i - 1];
                    float t = segment <= 0f ? 1f : (distance - cumulative[i - 1]) / segment;
                    return Vector2.Lerp(Path[i - 1], Path[i], t);
                }
            }
            return Path[Path.Count - 1];
        }

        /// <summary>점에서 경로(선분들)까지 가장 가까운 거리.</summary>
        public float DistanceTo(Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = 1; i < Path.Count; i++)
            {
                Vector2 a = Path[i - 1], ab = Path[i] - a;
                float lengthSq = ab.LengthSquared();
                float t = lengthSq <= 0f ? 0f : Math.Max(0f, Math.Min(1f, Vector2.Dot(point - a, ab) / lengthSq));
                best = Math.Min(best, Vector2.Distance(point, a + ab * t));
            }
            return best;
        }

        void EnsureCache()
        {
            if (cumulative != null && cumulative.Length == Path.Count) return;
            cumulative = new float[Path.Count];
            for (int i = 1; i < Path.Count; i++)
                cumulative[i] = cumulative[i - 1] + Vector2.Distance(Path[i - 1], Path[i]);
        }
    }

    /// <summary>건설 가능 영역(축 정렬 사각형). 타일 중심이 안에 있으면 건설 가능.</summary>
    public struct BuildZone
    {
        public float XMin, YMin, XMax, YMax;

        public BuildZone(float xMin, float yMin, float xMax, float yMax)
        {
            XMin = xMin; YMin = yMin; XMax = xMax; YMax = yMax;
        }

        public bool Contains(Vector2 p) => p.X >= XMin && p.X <= XMax && p.Y >= YMin && p.Y <= YMax;
    }

    public sealed class MapDef
    {
        public string Id;
        public Vector2 CorePos;
        public List<SpawnPointDef> SpawnPoints = new List<SpawnPointDef>();
        public List<BuildZone> BuildZones = new List<BuildZone>();
        /// <summary>카메라가 보여줄 영역 (x, y, 폭, 높이).</summary>
        public float CameraX, CameraY, CameraWidth, CameraHeight;
        /// <summary>건설 칸 중심이 길 중심선에서 이만큼(타일) 이상 떨어져야 한다. 0이면 검사하지 않는다(건설 구역만으로 판단).
        /// 그림 맵처럼 길이 구불구불해서 사각형 구역만으로 길을 피하기 어려울 때 쓴다.</summary>
        public float PathClearance;
        /// <summary>칸 맵(탐색형)처럼 칸마다 지을 수 있는지 정해진 맵. 있으면 건설 구역·길 여유 대신 이것으로 판단한다(월드 타일 좌표).</summary>
        public Func<int, int, bool> BuildCheck;

        public SpawnPointDef FindSpawn(string id) => SpawnPoints.Find(s => s.Id == id);

        public bool IsBuildable(Vector2 tileCenter)
        {
            if (BuildCheck != null) return BuildCheck((int)Math.Round(tileCenter.X), (int)Math.Round(tileCenter.Y));
            bool inZone = false;
            foreach (BuildZone zone in BuildZones)
                if (zone.Contains(tileCenter)) { inZone = true; break; }
            if (!inZone) return false;
            if (PathClearance <= 0f) return true;
            foreach (SpawnPointDef spawn in SpawnPoints)
                if (spawn.DistanceTo(tileCenter) < PathClearance) return false;
            return true;
        }
    }

    public sealed class SpawnGroupDef
    {
        public string Id;
        public string SpawnId;
        public EnemyDef Enemy;
        public int Count = 1;
        public float StartSec;
        public float IntervalSec = 1f;

        public float SpawnTime(int index) => StartSec + index * IntervalSec;
    }

    public sealed class WaveDef
    {
        public string Id;
        public float PrepareSec = 10f;
        public int ClearCoin;
        public List<SpawnGroupDef> Groups = new List<SpawnGroupDef>();
    }

    public sealed class StageDef
    {
        public string Id;
        public string Name;
        public MapDef Map;
        public List<WaveDef> Waves = new List<WaveDef>();
        public int StartCoin = 120;
        public int CoreMaxHp = 100;
        /// <summary>이 스테이지에서 나오는 모든 적의 HP 배율(같은 적 데이터를 맵 난이도에 맞춰 쓰기 위함).</summary>
        public float EnemyHpScale = 1f;
        /// <summary>이 스테이지에서 지을 수 있는 타워(기획서 Stage 테이블에 없는 프로토타입 추가 필드).</summary>
        public List<TowerDef> Towers = new List<TowerDef>();
    }
}
