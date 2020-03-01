using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoguelikeEngine.Effects;

namespace RoguelikeEngine
{
    struct ReusableID
    {
        public int ID;
        public int Generation;

        public ReusableID(int id, int generation)
        {
            ID = id;
            Generation = generation;
        }

        public ReusableID Next => new ReusableID(ID + 1, Generation);
        public ReusableID NextGeneration => new ReusableID(ID, Generation + 1);

        public static implicit operator int(ReusableID v)
        {
            return v.ID;
        }
    }

    static class EffectManager
    {
        class EffectList : List<Effect>
        {
            public int Generation;

            public EffectList(int generation)
            {
                Generation = generation;
            }
        }

        class IDArray
        {
            EffectList[] Lists;

            public IDArray(int capacity)
            {
                Capacity = capacity;
                Lists = new EffectList[16];
            }

            public int Capacity;

            public EffectList this[int index]
            {
                get
                {
                    Resize(index);
                    return Lists[index];
                }
                set
                {
                    Resize(index);
                    Lists[index] = value;
                }
            }

            private void Resize(int n)
            {
                bool changed = false;
                while (n >= Capacity)
                {
                    Capacity *= 2;
                    changed = true;
                }
                if(changed)
                    Array.Resize(ref Lists, Capacity);
            }
        }

        class Drawer
        {
            public IDArray Effects = new IDArray(16);

            public IEnumerable<Effect> Get(IEffectHolder holder)
            {
                if (Has(holder))
                    return Effects[holder.ObjectID].GetAndClean(effect => effect.Removed);
                else
                    return Enumerable.Empty<Effect>();
            }

            public bool Has(IEffectHolder holder)
            {
                EffectList list;
                return Effects.Capacity > holder.ObjectID && (list = Effects[holder.ObjectID]) != null && holder.ObjectID.Generation == list.Generation;
            }

            public void Add(IEffectHolder holder, Effect effect)
            {
                if (Has(holder))
                    Effects[holder.ObjectID].Add(effect);
                else
                    Effects[holder.ObjectID] = new EffectList(holder.ObjectID.Generation) { effect };
            }

            public void Remove(IEffectHolder holder, Effect effect)
            {
                if (Has(holder))
                    Effects[holder.ObjectID].Remove(effect);
            }
        }

        static ReusableID CurrentID = new ReusableID();
        static Queue<ReusableID> ReusableIDs = new Queue<ReusableID>();
        static Dictionary<Type, Drawer> Drawers = new Dictionary<Type, Drawer>();

        public static IEnumerable<T> GetEffects<T>(this IEffectHolder holder) where T : Effect
        {
            return GetDrawer(typeof(T)).Get(holder).OfType<T>();
        }

        private static Drawer GetDrawer(Type type)
        {
            if (!Drawers.ContainsKey(type))
                Drawers.Add(type, new Drawer());
            return Drawers[type];
        }

        public static void AddEffect(this IEffectHolder holder, Effect effect)
        {
            foreach (var type in Util.GetBaseTypes(effect))
            {
                GetDrawer(type).Add(holder,effect);
            }
        }

        public static void RemoveEffect(this IEffectHolder holder, Effect effect)
        {
            foreach (var type in Util.GetBaseTypes(effect))
            {
                GetDrawer(type).Remove(holder, effect);
            }
        }

        public static double GetStat(this IEffectHolder holder, Stat stat)
        {
            return CalculateStat(holder,holder.GetEffects<Effect>().Where(effect => effect is IStat statEffect && statEffect.Stat == stat),stat.DefaultStat);
        }

        public static Dictionary<Stat,double> GetStats(this IEffectHolder holder)
        {
            return holder.GetEffects<Effect>().OfType<IStat>().GroupBy(stat => stat.Stat, stat => (Effect)stat).ToDictionary(group => group.Key, group => CalculateStat(holder, group, group.Key.DefaultStat));
        }

        public static double CalculateStat(IEffectHolder holder, IEnumerable<Effect> effects, double defaultStat)
        {
            var groups = effects.ToTypeLookup();
            var baseStat = defaultStat + groups.Get<EffectStat>().Where(stat => stat.Holder == holder).Sum(stat => stat.Amount);
            var add = groups.Get<EffectStat>().Where(stat => stat.Holder != holder).Sum(stat => stat.Amount);
            var percentage = groups.Get<EffectStatPercent>().Sum(stat => stat.Percentage);
            var multiplier = groups.Get<EffectStatMultiply>().Aggregate(1.0, (seed, stat) => seed * stat.Multiplier);
            var locks = groups.Get<EffectStatLock>();
            var min = locks.Any() ? locks.Max(stat => stat.MinValue) : double.NegativeInfinity;
            var max = locks.Any() ? locks.Min(stat => stat.MaxValue) : double.PositiveInfinity;

            return Math.Max(min, Math.Min((baseStat + percentage * baseStat + add) * multiplier, max));
        }

        public static void TakeDamage(this IEffectHolder holder, double damage, Element element)
        {
            Effect.Apply(new EffectDamage(holder, damage, element));
        }

        public static void OnDefend(this IEffectHolder holder, Attack attack)
        {
            foreach (var onDefend in holder.GetEffects<OnDefend>())
            {
                onDefend.Trigger(attack);
            }
        }

        public static void OnStartDefend(this IEffectHolder holder, Attack attack)
        {
            foreach (var onStartDefend in holder.GetEffects<OnStartDefend>())
            {
                onStartDefend.Trigger(attack);
            }
        }

        public static void ClearStatusEffects(this IEffectHolder holder)
        {
            foreach (var effect in holder.GetEffects<EffectStatusEffect>())
                effect.Remove();
        }

        public static void AddStatusEffect(this IEffectHolder holder, StatusEffect statusEffect)
        {
            statusEffect.Creature = holder;
            var statusEffects = holder.GetEffects<EffectStatusEffect>();
            var combineable = statusEffects.Select(x => x.StatusEffect).Where(x => x.CanCombine(statusEffect)).ToList();
            if (combineable.Any())
            {
                var combined = combineable.SelectMany(x => x.Combine(statusEffect)).Distinct().ToList();
                var added = combined.Except(combineable).ToList();
                var removed = combineable.Except(combined).ToHashSet();
                foreach (var effect in added)
                    effect.Apply();
                foreach (var effect in statusEffects.Where(x => removed.Contains(x.StatusEffect)))
                    effect.Remove();
            }
            else
            {
                statusEffect.Apply();
            }
        }

        public static ReusableID NewID()
        {
            if (ReusableIDs.Count > 0)
                return ReusableIDs.Dequeue();
            CurrentID = CurrentID.Next;
            return CurrentID;
        }

        public static void DeleteHolder(IEffectHolder holder)
        {
            ReusableIDs.Enqueue(holder.ObjectID.NextGeneration);
        }
    }
}
