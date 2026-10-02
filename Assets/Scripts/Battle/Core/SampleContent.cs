using System.Collections.Generic;
using System.Numerics;

namespace Akmong.Battle
{
    /// <summary>
    /// 시스템 기획서 5장의 샘플 데이터(적·타워·맵은 기획서 값 그대로).
    /// 자동 테스트와 Unity 에셋 생성(처음 한 번)에 같이 쓴다.
    /// </summary>
    public static class SampleContent
    {
        public static EnemyDef Toy() => new EnemyDef { Id = "EN_TOY", Name = "병정", MaxHp = 100, Armor = 25, MoveSpeed = 1.5f, CoreDamage = 5, KillCoin = 3 };
        public static EnemyDef Rush() => new EnemyDef { Id = "EN_RUSH", Name = "빠른 그림자", MaxHp = 60, Armor = 0, MoveSpeed = 2.5f, CoreDamage = 4, KillCoin = 3 };
        public static EnemyDef Heavy() => new EnemyDef { Id = "EN_HEAVY", Name = "무거운 봉제", MaxHp = 260, Armor = 50, MoveSpeed = 0.8f, CoreDamage = 12, KillCoin = 8 };

        public static TowerDef Lamp() => new TowerDef
        {
            Id = "TW_LAMP",
            Name = "스탠드",
            BuildCost = 50,
            Levels =
            {
                new TowerLevelDef { Id = "TL_LAMP_1", Damage = 20, Range = 4.0f, AttackSec = 1.0f, CritChance = 0.10f, CritMult = 1.5f, UpgradeCost = 70 },
                new TowerLevelDef { Id = "TL_LAMP_2", Damage = 34, Range = 4.5f, AttackSec = 0.9f, CritChance = 0.10f, CritMult = 1.5f, UpgradeCost = 0 },
            },
        };

        /// <summary>
        /// 병정인형: 근거리 저지(타워 역할 기획서). 접근한 적을 붙잡아 다른 타워가 칠 시간을 번다.
        /// 2단계는 방패로 더 많이·오래 붙잡고, 3단계는 방패 밀치기로 일반 적을 뒤로 민다(보스 면역).
        /// 수치는 프로토타입 임시값(밸런싱에서 확정).
        /// </summary>
        public static TowerDef Soldier() => new TowerDef
        {
            Id = "TW_SOLDIER",
            Name = "병정인형",
            BuildCost = 40,
            Levels =
            {
                new TowerLevelDef { Id = "TL_SOLDIER_1", Damage = 16, Range = 1.5f, AttackSec = 0.8f, CritChance = 0.05f, CritMult = 1.5f, UpgradeCost = 60, BlockCount = 1, BlockSec = 3f },
                new TowerLevelDef { Id = "TL_SOLDIER_2", Damage = 22, Range = 1.5f, AttackSec = 0.8f, CritChance = 0.05f, CritMult = 1.5f, UpgradeCost = 90, BlockCount = 2, BlockSec = 4f },
                new TowerLevelDef { Id = "TL_SOLDIER_3", Damage = 30, Range = 1.6f, AttackSec = 0.75f, CritChance = 0.05f, CritMult = 1.5f, UpgradeCost = 0, BlockCount = 2, BlockSec = 4f, Knockback = 0.6f },
            },
        };

        /// <summary>MAP_ROOM: 북쪽 (0,12)와 동쪽 (12,0)에서 중심 (0,0)까지 길이 12인 직선 경로 2개.</summary>
        public static MapDef Room() => new MapDef
        {
            Id = "MAP_ROOM",
            CorePos = Vector2.Zero,
            SpawnPoints =
            {
                new SpawnPointDef { Id = "SP_ROOM_N", Path = { new Vector2(0, 12), new Vector2(0, 0) } },
                new SpawnPointDef { Id = "SP_ROOM_E", Path = { new Vector2(12, 0), new Vector2(0, 0) } },
            },
            BuildZones =
            {
                new BuildZone(-3, 2, -1, 11),  // 북쪽 길 왼편
                new BuildZone(1, 2, 3, 11),    // 북쪽 길 오른편
                new BuildZone(4, 1, 11, 3),    // 동쪽 길 위
                new BuildZone(2, -3, 11, -1),  // 동쪽 길 아래
            },
            CameraX = -8, CameraY = -4, CameraWidth = 26, CameraHeight = 17,
        };

        /// <summary>
        /// 플레이용 첫 의뢰 스테이지(프로토타입 1차 밸런스).
        /// 기획서 샘플 웨이브(SpecSampleStage)는 시작 재화 120으로 막기 어려운 스트레스 테스트라서
        /// 적 수와 간격을 줄였다. 모의 플레이(봇 200회) 기준 성공률 약 73%, 총 소요 약 2분(준비 포함).
        /// </summary>
        public static StageDef StageQ01()
        {
            EnemyDef toy = Toy(), rush = Rush(), heavy = Heavy();
            return new StageDef
            {
                Id = "STG_Q01",
                Name = "첫 의뢰",
                Map = Room(),
                StartCoin = 120,
                CoreMaxHp = 100,
                Towers = { Lamp(), Soldier() },
                Waves =
                {
                    new WaveDef
                    {
                        Id = "W_Q01_01", PrepareSec = 15, ClearCoin = 20,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_Q01_1N", SpawnId = "SP_ROOM_N", Enemy = toy, Count = 4, StartSec = 0, IntervalSec = 3 },
                            new SpawnGroupDef { Id = "SG_Q01_1E", SpawnId = "SP_ROOM_E", Enemy = rush, Count = 2, StartSec = 6, IntervalSec = 3 },
                        },
                    },
                    new WaveDef
                    {
                        Id = "W_Q01_02", PrepareSec = 15, ClearCoin = 25,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_Q01_2N", SpawnId = "SP_ROOM_N", Enemy = toy, Count = 6, StartSec = 0, IntervalSec = 3 },
                            new SpawnGroupDef { Id = "SG_Q01_2E", SpawnId = "SP_ROOM_E", Enemy = rush, Count = 4, StartSec = 4, IntervalSec = 3.5f },
                        },
                    },
                    new WaveDef
                    {
                        Id = "W_Q01_03", PrepareSec = 15, ClearCoin = 30,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_Q01_3N", SpawnId = "SP_ROOM_N", Enemy = toy, Count = 8, StartSec = 0, IntervalSec = 2.8f },
                            new SpawnGroupDef { Id = "SG_Q01_3E", SpawnId = "SP_ROOM_E", Enemy = rush, Count = 6, StartSec = 3, IntervalSec = 3 },
                            new SpawnGroupDef { Id = "SG_Q01_3H", SpawnId = "SP_ROOM_N", Enemy = heavy, Count = 1, StartSec = 14, IntervalSec = 1 },
                        },
                    },
                },
            };
        }

        /// <summary>기획서 5장 샘플 그대로: 웨이브 1개(북 병정 8, 동 그림자 4). 검산 테스트 전용.</summary>
        public static StageDef SpecSampleStage()
        {
            StageDef stage = StageQ01();
            stage.Id = "STG_SPEC";
            stage.Waves = new List<WaveDef>
            {
                new WaveDef
                {
                    Id = "W_Q01_01", PrepareSec = 10, ClearCoin = 20,
                    Groups =
                    {
                        new SpawnGroupDef { Id = "SG_Q01_N", SpawnId = "SP_ROOM_N", Enemy = Toy(), Count = 8, StartSec = 0, IntervalSec = 1.5f },
                        new SpawnGroupDef { Id = "SG_Q01_E", SpawnId = "SP_ROOM_E", Enemy = Rush(), Count = 4, StartSec = 4, IntervalSec = 2 },
                    },
                },
            };
            return stage;
        }
    }
}
