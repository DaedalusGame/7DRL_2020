using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.VisualEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class AttackSpecial
    {
        public abstract Wait Start(Attack attack);

        public abstract Wait End(Attack attack);
    }

    class AttackPhysical : AttackSpecial
    {
        public override Wait End(Attack attack)
        {
            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackEndFunction : AttackSpecial
    {
        Func<Attack, IEnumerable<Wait>> Function;

        public AttackEndFunction(Func<Attack, IEnumerable<Wait>> function)
        {
            Function = function;
        }

        public override Wait End(Attack attack)
        {
            return Scheduler.Instance.RunAndWait(Function(attack));
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackDrain : AttackSpecial
    {
        double Rate;

        public AttackDrain(double rate)
        {
            Rate = rate;
        }

        public override Wait End(Attack attack)
        {
            foreach (var damage in attack.FinalDamage)
            {
                if (damage.Value > 0)
                    attack.Attacker.Heal(damage.Value * Rate);
                else if (damage.Value < 0)
                    attack.Attacker.TakeDamage(-damage.Value * Rate, damage.Key);
            }

            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackWeapon : AttackSpecial
    {
        public Item Weapon;

        public AttackWeapon(Item weapon)
        {
            Weapon = weapon;
        }

        public override Wait End(Attack attack)
        {
            Weapon.TakeDamage(1, Element.Bludgeon, null);

            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackSkill : AttackSpecial
    {
        public Skill Skill;

        public AttackSkill(Skill skill)
        {
            Skill = skill;
        }

        public override Wait End(Attack attack)
        {
            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackProjectile : AttackSpecial
    {
        Item ProjectileWeapon;

        public AttackProjectile(Item weapon)
        {
            ProjectileWeapon = weapon;
        }

        public override Wait End(Attack attack)
        {
            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackArmor : AttackSpecial
    {
        IEnumerable<Item> Armors;

        public AttackArmor(IEnumerable<Item> armors)
        {
            Armors = armors;
        }

        public override Wait End(Attack attack)
        {
            foreach(Item item in Armors)
            {
                item.TakeDamage(1, Element.Bludgeon, null);
            }

            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class AttackDamageStat : AttackSpecial
    {
        Stat Stat;
        double Rate;

        public AttackDamageStat(Stat stat, double rate)
        {
            Stat = stat;
            Rate = rate;
        }

        public override Wait End(Attack attack)
        {
            var totalDamage = attack.FinalDamage.Sum(x => x.Value);

            attack.Defender.TakeStatDamage(Rate * totalDamage, Stat);

            return Wait.NoWait;
        }

        public override Wait Start(Attack attack)
        {
            return Wait.NoWait;
        }
    }

    class Attack
    {
        public delegate VisualPreset.AtCreature HitGenerator(SceneGame world);

        public static Dictionary<Element, HitGenerator> ElementMap = new Dictionary<Element, HitGenerator>()
        {
            { Element.Bludgeon, (world) => new HitBludgeon(world) },
            { Element.Slash, (world) => new HitSlash(world) },
            { Element.Pierce, (world) => new HitPierce(world) },

            { Element.Fire, (world) => new HitFire(world) },
            { Element.Ice, (world) => new HitIce(world) },
            { Element.Thunder, (world) => new HitThunder(world) },
            { Element.Water, (world) => new HitWater(world) },
            { Element.Wind, (world) => new HitWind(world) },
            { Element.Earth, (world) => new HitEarth(world) },
            { Element.Holy, (world) => new HitHoly(world) },
            { Element.Dark, (world) => new HitDark(world) },

            { Element.Poison, (world) => new HitPoison(world) },
            { Element.Acid, (world) => new HitAcid(world) },
            { Element.Bleed, (world) => new HitBlood(world) },

            { Element.Hellfire, (world) => new HitHellfire(world) },
            { Element.Light, (world) => new HitLight(world) },
            { Element.Drought, (world) => new HitDrought(world) },
            { Element.Inferno, (world) => new HitInferno(world) },
            { Element.Blizzard, (world) => new HitBlizzard(world) },
            { Element.BlackFlame, (world) => new HitBlackFlame(world) },
            { Element.Arcane, (world) => new HitArcane(world) },

            { Element.Chaos, (world) => new HitChaos(world) },
        };

        public Creature Attacker;
        public IEffectHolder Defender;

        Random Random = new Random(); //TODO: Remove random in favor of determinism

        public Dictionary<Element, double> Elements = new Dictionary<Element, double>();
        public List<StatusEffect> StatusEffects = new List<StatusEffect>();
        public List<AttackSpecial> ExtraEffects = new List<AttackSpecial>();

        public double Force = 0;
        public double AttackModifier = 1;
        public double DefenseModifier = 1;
        public double ResistanceModifier = 1;

        public bool IgnoreElementRate;
        public bool Unblockable;

        public bool Blocked;
        public bool CritBlocked;
        public Dictionary<Element, double> BlockedDamage = new Dictionary<Element, double>();

        public IEnumerable<Element> SplitElements => Deflected.Keys;
        public Dictionary<Element, double> Deflected = new Dictionary<Element, double>();
        
        public IEffectHolder Fault;
        public int ReactionLevel;
        public double Damage;
        public Dictionary<Element, double> FinalDamage = new Dictionary<Element, double>();
        public PopupHelper PopupHelper = PopupHelper.Global;

        public bool CheckDeath = true;
        public Vector2 HitDirection;

        public List<VisualPreset.AtCreature> HitEffects = new List<VisualPreset.AtCreature>(); //Hit effects played on hit
        public VisualPreset.AtCreature DamageEffect; //Damage spark effect played at a short delay
        public bool NoStandardEffect; //Prevents standard element effects from being added

        List<Wait> Waits = new List<Wait>();

        public Attack(Creature attacker, IEffectHolder defender)
        {
            Attacker = attacker;
            Defender = defender;
            DamageEffect = new HitDamageSpark(attacker.World);
        }

        public void SetParameters(double force, double attackMod, double defenseMod)
        {
            Force = force;
            AttackModifier = attackMod;
            DefenseModifier = defenseMod;
        }

        public virtual IEnumerable<Wait> RoutineStart()
        {
            CalculateDamage();
            CalculateArmor();

            yield return Attacker.OnStartAttack(this);
            yield return Defender.OnStartDefend(this);

            List<Wait> waits = new List<Wait>();
            foreach(var effect in ExtraEffects)
            {
                waits.Add(effect.Start(this));
            }
            yield return new WaitAll(waits);

            FinalDamage = Elements.ToDictionary(pair => pair.Key, pair => CalculateSplitElement(pair.Key, pair.Value * Damage));

            if(!Unblockable)
            {
                var blockChance = Defender.GetStat(Stat.BlockChance);
                var critBlockChance = Defender.GetStat(Stat.CritBlockChance);

                CheckBlock(blockChance, critBlockChance, ref Blocked, ref CritBlocked);
            }

            if (Blocked || CritBlocked)
            {
                var blockValue = Defender.GetStat(Stat.BlockValue);
                var blockRate = Defender.GetStat(Stat.BlockRate);

                BlockDamage(blockValue, blockRate);
                PopupHelper.Add(new MessageText(Defender, "Blocked!"));
            }

            foreach (var damage in FinalDamage)
            {
                if (damage.Value >= 0)
                    Defender.TakeDamage(damage.Value, damage.Key, PopupHelper);
                else
                    Defender.Heal(-damage.Value, PopupHelper);
            }
            foreach (var statusEffect in StatusEffects)
                Defender.AddStatusEffect(statusEffect);
            double total = FinalDamage.Sum(x => Math.Abs(x.Value));
            Effect.Apply(new EffectLastHit(Defender, Attacker, total));

            if (Defender is Creature creature)
            {
                if (HitEffects.Empty() && !NoStandardEffect)
                    GenerateHitEffects(creature);
                Scheduler.Instance.Run(RoutineHitEffects(creature));
            }

            waits.Clear();
            foreach (var effect in ExtraEffects)
            {
                waits.Add(effect.End(this));
            }
            yield return new WaitAll(waits);

            yield return Attacker.OnAttack(this);
            yield return Defender.OnDefend(this);

            yield return new WaitAll(Waits);

            if(CheckDeath && Defender is Creature targetCreature)
                targetCreature.CheckDead(HitDirection);
        }

        private void CheckBlock(double blockChance, double critBlockChance, ref bool blocked, ref bool critBlocked)
        {
            var pickedValue = Random.NextDouble();

            if (pickedValue < critBlockChance)
                critBlocked = true;
            if (pickedValue < blockChance)
                blocked = true;
        }

        private void BlockDamage(double blockValue, double blockRate)
        {
            var positiveDamages = FinalDamage.Where(x => x.Value > 0).ToList();

            var reducedDamage = blockValue + positiveDamages.Sum(x => x.Value) * blockRate;

            var resistedDamages = Util.ProportionalSplit(positiveDamages.Select(x => x.Value), reducedDamage);
            int i = 0;
            foreach (var resist in resistedDamages)
            {
                var element = positiveDamages[i].Key;
                BlockedDamage.Add(element, resist);
                FinalDamage[element] = Math.Max(FinalDamage[element] - resist, 0);
                i++;
            }
        }

        private IEnumerable<Wait> RoutineHitEffects(Creature creature)
        {
            foreach(var effect in HitEffects)
            {
                effect.Activate(creature);
            }
            yield return new WaitTime(10);
            DamageEffect?.Activate(creature);
        }

        private void GenerateHitEffects(Creature creature)
        {
            foreach (var damage in FinalDamage.OrderByDescending(x => x.Value))
            {
                if (damage.Value > 0)
                {
                    var func = ElementMap.GetOrDefault(damage.Key, null);
                    if (func != null)
                    {
                        var hitEffect = func.Invoke(creature.World);
                        HitEffects.Add(hitEffect);
                    }
                }
            }
        }

        private void CalculateArmor()
        {
            var equipment = Defender.GetEffects<EffectItemEquipped>();
            var mainhand = equipment.FirstOrDefault(x => x.Slot == EquipSlot.Mainhand)?.Item;
            var offhand = equipment.FirstOrDefault(x => x.Slot == EquipSlot.Offhand)?.Item;
            var body = equipment.FirstOrDefault(x => x.Slot == EquipSlot.Body)?.Item;

            List<Item> armors = new List<Item>();

            //TODO: Should have some other check to find out if an item is a shield/armor
            if (mainhand is ToolPlate)
                armors.Add(mainhand);
            if (offhand is ToolPlate)
                armors.Add(offhand);
            if (body is ToolPlate)
                armors.Add(body);

            if (armors.Any())
                ExtraEffects.Add(new AttackArmor(armors));
        }

        private void CalculateDamage()
        {
            double attack = Attacker.GetStat(Stat.Attack) * AttackModifier + Force;
            double defense = Defender.GetStat(Stat.Defense) * DefenseModifier;

            Damage = Math.Max(attack - defense, 0) * Defender.GetStat(Stat.DamageRate);
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
            if (IgnoreElementRate)
                damageRate = 1;
            double resistance = Defender.GetStat(element.Resistance) * ResistanceModifier;
            double finalDamage = Math.Max(0, damage - resistance) * damageRate;
            Deflected[element] = Deflected.GetOrDefault(element, 0) + (damage - finalDamage); //Keep track of how much damage we reduced/increased
            return finalDamage;
        }
    }
}
