using System;

namespace Akmong.Battle
{
    /// <summary>한 번의 타격 계산에 들어가는 값 (시스템 기획서 4장).</summary>
    public struct DamageInput
    {
        public float BaseDamage;
        public float DamageAdd;
        /// <summary>공격력 증가율의 합. 0.2 = +20%.</summary>
        public float DamagePct;
        public float Armor;
        public float ArmorAdd;
        public float Vulnerability;
        public float Reduction;
        public bool Crit;
        public float CritMult;
    }

    /// <summary>상태가 없는 계산 함수 모음. 같은 입력이면 항상 같은 결과.</summary>
    public static class BattleMath
    {
        /// <summary>
        /// D = floor(A × C × M × (1 + vulnerability) × (1 − reduction) + 0.5)
        /// A = max(0, (base + add) × (1 + pct)), M = 100 / (100 + R)
        /// </summary>
        public static int ResolveDamage(DamageInput input, GameRules rules)
        {
            double pct = Math.Max(rules.MinDamagePct, input.DamagePct);
            double attack = Math.Max(0.0, ((double)input.BaseDamage + input.DamageAdd) * (1.0 + pct));
            double armor = Math.Max(0.0, (double)input.Armor + input.ArmorAdd);
            double crit = input.Crit ? input.CritMult : 1.0;
            double mitigation = rules.ArmorConstant / (rules.ArmorConstant + armor);
            double vulnerability = Clamp(input.Vulnerability, 0.0, 1.0);
            double reduction = Clamp(input.Reduction, 0.0, 0.8);

            int damage = (int)Math.Floor(attack * crit * mitigation * (1.0 + vulnerability) * (1.0 - reduction) + 0.5);
            // 양수 공격은 최소 1, 기본·가산 공격력이 모두 0인 효과 전용 공격은 0.
            bool positiveAttack = input.BaseDamage + input.DamageAdd > 0f && attack > 0.0;
            return positiveAttack ? Math.Max(1, damage) : 0;
        }

        /// <summary>speed = base × clamp(tier × haste × slow, 0.25, 2.0). 속박이면 0.</summary>
        public static float EffectiveSpeed(float baseSpeed, float tierMult, float haste, float slow, bool rooted, GameRules rules)
        {
            if (rooted) return 0f;
            float ratio = Clamp(tierMult * haste * slow, rules.MinMoveRatio, rules.MaxMoveRatio);
            return baseSpeed * ratio;
        }

        /// <summary>최종 공격 간격 = max(0.20, attack_sec).</summary>
        public static float AttackInterval(float attackSec, GameRules rules) => Math.Max(rules.MinAttackSec, attackSec);

        public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;

        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }

    /// <summary>재현 가능한 난수(치명타 판정). 테스트에서는 고정값을 넣는다.</summary>
    public interface IRandom
    {
        double NextDouble();
    }

    public sealed class SeededRandom : IRandom
    {
        readonly Random random;

        public SeededRandom(int seed)
        {
            Seed = seed;
            random = new Random(seed);
        }

        public int Seed { get; }

        public double NextDouble() => random.NextDouble();
    }
}
