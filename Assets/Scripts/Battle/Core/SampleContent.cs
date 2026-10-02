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
        /// MAP_GARDEN: 달빛 재봉 정원(맵 시안 1672×941, 1타일 = 75px). 오른쪽 아래 입구에서 시작해
        /// 바깥 고리 → 가운데 고리 → 안쪽 S자를 돌아 정원 한가운데(꿈의 중심)에 닿는 한 줄 길(약 49타일).
        /// 좌표: x = 픽셀/75, y = (941 - 픽셀)/75. 타워는 풀밭 구역 안, 길 중심에서 0.8타일 이상 떨어진 칸에 짓는다.
        /// </summary>
        public static MapDef Garden()
        {
            var path = new List<Vector2>();
            int[,] px =
            {
                // 그림에서 돌길 중심선을 따 왔다(오른쪽 바깥 길은 원근 때문에 아래로 갈수록 오른쪽으로 기운다).
                { 1140, 725 }, { 1295, 722 }, { 1320, 690 }, { 1318, 640 }, { 1268, 268 }, { 1245, 244 }, { 450, 244 },
                { 428, 266 }, { 428, 620 }, { 450, 641 }, { 1085, 641 }, { 1105, 620 }, { 1105, 388 }, { 1085, 366 },
                { 680, 366 }, { 660, 388 }, { 660, 476 }, { 680, 497 }, { 925, 497 },
            };
            for (int i = 0; i < px.GetLength(0); i++) path.Add(new Vector2(px[i, 0] / 75f, (941 - px[i, 1]) / 75f));
            return new MapDef
            {
                Id = "MAP_GARDEN",
                CorePos = path[path.Count - 1],
                SpawnPoints = { new SpawnPointDef { Id = "SP_GARDEN", Path = path } },
                BuildZones = { new BuildZone(4.4f, 3.0f, 18.7f, 10.6f) }, // 길이 지나는 풀밭(울타리 안쪽)
                PathClearance = 0.8f,
                CameraX = 0f, CameraY = 0f, CameraWidth = 1672f / 75f, CameraHeight = 941f / 75f,
            };
        }

        /// <summary>
        /// 첫 의뢰(정원 맵): 길이 길고(약 49타일) 한 타워가 여러 줄을 칠 수 있어서 MAP_ROOM보다 적이 많고 촘촘하다.
        /// 5웨이브, 적 HP 1.55배. 모의 플레이(봇 300회) 성공률: 병정인형 섞어 짓기 약 79%, 스탠드만 약 90%.
        /// 길이 길어 한 판이 약 5~6분으로 기획 목표(2~3분)보다 길다.
        /// </summary>
        public static StageDef StageGarden()
        {
            EnemyDef toy = Toy(), rush = Rush(), heavy = Heavy();
            return new StageDef
            {
                Id = "STG_Q01",
                Name = "달빛 재봉 정원",
                Map = Garden(),
                StartCoin = 120,
                CoreMaxHp = 100,
                EnemyHpScale = 1.55f, // 길이 길어 맞는 시간이 길므로 적 HP를 올린다
                Towers = { Lamp(), Soldier() },
                Waves =
                {
                    new WaveDef
                    {
                        Id = "W_GD_01", PrepareSec = 20, ClearCoin = 20,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_GD1_1", SpawnId = "SP_GARDEN", Enemy = toy, Count = 6, StartSec = 0f, IntervalSec = 2f },
                            new SpawnGroupDef { Id = "SG_GD1_2", SpawnId = "SP_GARDEN", Enemy = rush, Count = 3, StartSec = 5f, IntervalSec = 2.5f },
                        },
                    },
                    new WaveDef
                    {
                        Id = "W_GD_02", PrepareSec = 15, ClearCoin = 25,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_GD2_1", SpawnId = "SP_GARDEN", Enemy = toy, Count = 10, StartSec = 0f, IntervalSec = 1.6f },
                            new SpawnGroupDef { Id = "SG_GD2_2", SpawnId = "SP_GARDEN", Enemy = rush, Count = 6, StartSec = 4f, IntervalSec = 1.8f },
                        },
                    },
                    new WaveDef
                    {
                        Id = "W_GD_03", PrepareSec = 15, ClearCoin = 30,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_GD3_1", SpawnId = "SP_GARDEN", Enemy = toy, Count = 12, StartSec = 0f, IntervalSec = 1.3f },
                            new SpawnGroupDef { Id = "SG_GD3_2", SpawnId = "SP_GARDEN", Enemy = rush, Count = 8, StartSec = 3f, IntervalSec = 1.5f },
                            new SpawnGroupDef { Id = "SG_GD3_3", SpawnId = "SP_GARDEN", Enemy = heavy, Count = 2, StartSec = 12f, IntervalSec = 6f },
                        },
                    },
                    new WaveDef
                    {
                        Id = "W_GD_04", PrepareSec = 15, ClearCoin = 35,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_GD4_1", SpawnId = "SP_GARDEN", Enemy = rush, Count = 14, StartSec = 0f, IntervalSec = 1f },
                            new SpawnGroupDef { Id = "SG_GD4_2", SpawnId = "SP_GARDEN", Enemy = heavy, Count = 3, StartSec = 5f, IntervalSec = 5f },
                        },
                    },
                    new WaveDef
                    {
                        Id = "W_GD_05", PrepareSec = 15, ClearCoin = 40,
                        Groups =
                        {
                            new SpawnGroupDef { Id = "SG_GD5_1", SpawnId = "SP_GARDEN", Enemy = toy, Count = 16, StartSec = 0f, IntervalSec = 1f },
                            new SpawnGroupDef { Id = "SG_GD5_2", SpawnId = "SP_GARDEN", Enemy = rush, Count = 12, StartSec = 4f, IntervalSec = 1.1f },
                            new SpawnGroupDef { Id = "SG_GD5_3", SpawnId = "SP_GARDEN", Enemy = heavy, Count = 5, StartSec = 8f, IntervalSec = 4f },
                        },
                    },
                },
            };
        }

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
