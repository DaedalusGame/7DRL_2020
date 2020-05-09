using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Attack
    {
        public Creature Attacker;
        public IEffectHolder Defender;

        public Dictionary<Element, double> Elements = new Dictionary<Element, double>();
        public List<StatusEffect> StatusEffects = new List<StatusEffect>();

        public int ReactionLevel;
        public double Damage;
        public Dictionary<Element, double> FinalDamage = new Dictionary<Element, double>();

        public Attack(Creature attacker, IEffectHolder defender)
        {
            Attacker = attacker;
            Defender = defender;
        }

        public virtual void Start()
        {
            CalculateDamage();

            Attacker.OnStartAttack(this);
            Defender.OnStartDefend(this);

            FinalDamage = Elements.ToDictionary(pair => pair.Key, pair => CalculateSplitElement(pair.Key, pair.Value * Damage));

            foreach (var damage in FinalDamage)
            {
                if (damage.Value >= 0)
                    Defender.TakeDamage(damage.Value, damage.Key);
                else
                    Defender.Heal(-damage.Value);
            }
            foreach (var statusEffect in StatusEffects)
                Defender.AddStatusEffect(statusEffect);

            Attacker.OnAttack(this);
            Defender.OnDefend(this);
        }

        protected virtual void CalculateDamage()
        {
            double attack = Attacker.GetStat(Stat.Attack);
            double defense = Defender.GetStat(Stat.Defense);

            Damage = Math.Max(attack - defense, 0);
        }

        private double CalculateSplitElement(Element element, double damage)
        {
            Dictionary<Element, double> finalDamage = new Dictionary<Element, double>() { { element, damage } };
            while (finalDamage.Any(pair => pair.Key.CanSplit()))
                finalDamage = SplitElementalDamage(finalDamage);
            finalDamage = CalculateElementalDamage(finalDamage);
            return finalDamage.Sum(x => x.Value);
        }

        private Dictionary<Element, double> SplitElementalDamage(IDictionary<Element, double> elementDamage)
        {
            Dictionary<Element, double> finalDamage = new Dictionary<Element, double>();

            foreach(var pair in elementDamage)
            {
                if (pair.Key.CanSplit())
                {
                    double toSplit = CalculateElementalDamagePart(pair.Key, pair.Value);
                    foreach (var split in pair.Key.Split())
                    {
                        double damage = finalDamage.GetOrDefault(split.Key, 0);
                        finalDamage[split.Key] = damage + pair.Value * split.Value;
                    }
                }
                else
                {
                    double damage = finalDamage.GetOrDefault(pair.Key, 0);
                    finalDamage[pair.Key] = damage + pair.Value;
                }
            }

            return finalDamage;
        }

        private Dictionary<Element, double> CalculateElementalDamage(IDictionary<Element, double> elementDamage)
        {
            return elementDamage.ToDictionary(pair => pair.Key, pair => CalculateElementalDamagePart(pair.Key, pair.Value));
        }

        private double CalculateElementalDamagePart(Element element, double damage)
        {
            double damageRate = Defender.GetStat(element.DamageRate);
            double resistance = Defender.GetStat(element.Resistance);
            return Math.Max(0, damage - resistance) * damageRate;
        }
    }

    class AttackDrain : Attack
    {
        double Rate;

        public AttackDrain(Creature attacker, IEffectHolder defender, double rate) : base(attacker, defender)
        {
            Rate = rate;
        }

        public override void Start()
        {
            base.Start();

            foreach (var damage in FinalDamage)
            {
                if (damage.Value > 0)
                    Attacker.Heal(damage.Value * Rate);
                else if(damage.Value < 0)
                    Attacker.TakeDamage(-damage.Value * Rate, damage.Key);
            }
        }
    }

    class AttackItem : Attack
    {
        public Item Weapon;

        public AttackItem(Creature attacker, Item weapon, IEffectHolder defender) : base(attacker, defender)
        {
            Weapon = weapon;
        }

        public override void Start()
        {
            
        }
    }
}
