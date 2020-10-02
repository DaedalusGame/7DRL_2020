using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.Traits;

namespace RoguelikeEngine
{
    struct ReusableID
    {
        public static ReusableID Null = new ReusableID(-1, 0);

        public int ID;
        public int Generation;
        public bool Valid => ID >= 0 && EffectManager.HasHolder(this);

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

    class EffectHolderRegistry
    {
        ReusableID CurrentID = new ReusableID();
        Queue<ReusableID> ReusableIDs = new Queue<ReusableID>();
        BiDictionary<int, ReusableID> IntToID = new BiDictionary<int, ReusableID>();
        BiDictionary<Guid, ReusableID> GlobalIDToID = new BiDictionary<Guid, ReusableID>();
        Dictionary<ReusableID, WeakReference<IEffectHolder>> Holders = new Dictionary<ReusableID, WeakReference<IEffectHolder>>();
        int HighestPersistentID;

        public ReusableID SetID(IEffectHolder holder)
        {
            var id = NewID();
            Holders.Add(id, new WeakReference<IEffectHolder>(holder));
            IntToID.Add(id.ID, id);
            if (holder is IEffectHolderPersistent)
                HighestPersistentID = Math.Max(HighestPersistentID, id);
            return id;
        }

        public Guid SetGlobalID(IEffectHolder holder)
        {
            if (holder is IJsonSerializable serializable) {
                Guid globalId = Guid.NewGuid();
                GlobalIDToID.Remove(serializable.GlobalID);
                GlobalIDToID.Add(globalId, holder.ObjectID);
                return globalId;
            }
            return Guid.Empty;
        }

        public Guid SetGlobalID(IEffectHolder holder, Guid globalId)
        {
            if (holder is IJsonSerializable serializable)
            {
                //TODO: Handle Guid collisions (yeah right...)
                GlobalIDToID.Remove(serializable.GlobalID);
                GlobalIDToID.Add(globalId, holder.ObjectID);
                return globalId;
            }
            return Guid.Empty;
        }

        public bool Has(ReusableID id)
        {
            WeakReference<IEffectHolder> weakref;
            if(Holders.TryGetValue(id, out weakref))
            {
                IEffectHolder holder;
                if (weakref.TryGetTarget(out holder))
                    return true;
                else
                    Remove(id);
            }
            return false;
        }

        public bool Has(int id)
        {
            return IntToID.Forward.Contains(id) && Has(IntToID.Forward[id]);
        }

        public IEffectHolder Get(ReusableID id)
        {
            WeakReference<IEffectHolder> weakref;
            if (Holders.TryGetValue(id, out weakref))
            {
                IEffectHolder holder;
                if (weakref.TryGetTarget(out holder))
                    return holder;
                else
                    Remove(id);
            }
            return null;
        }

        public IEffectHolder Get(int id)
        {
            return Get(IntToID.Forward[id]);
        }

        public IEffectHolder Get(Guid globalId)
        {
            return Get(GlobalIDToID.Forward[globalId]);
        }

        public void Remove(IEffectHolder holder)
        {
            Remove(holder.ObjectID);
        }

        public void Remove(ReusableID id)
        {
            ReturnID(id);
            IntToID.Remove(id);
            GlobalIDToID.Remove(id);
            Holders.Remove(id);
        }

        private ReusableID NewID()
        {
            if (ReusableIDs.Count > 0)
            {
                Console.WriteLine("Reusing ID");
                return ReusableIDs.Dequeue();
            }
            CurrentID = CurrentID.Next;
            return CurrentID;
        }

        private void ReturnID(ReusableID id)
        {
            Debug.Assert(!ReusableIDs.Contains(id.NextGeneration));
            if (id == ReusableID.Null)
                return;
            ReusableIDs.Enqueue(id.NextGeneration);
        }

        private IEnumerable<ReusableID> GetNonPersistentIDs()
        {
            List<ReusableID> ids = new List<ReusableID>();
            foreach(var pair in Holders)
            {
                IEffectHolder holder;
                if (!pair.Value.TryGetTarget(out holder) || !(holder is IEffectHolderPersistent))
                    ids.Add(pair.Key);
            }
            return ids;
        }

        public void Clear()
        {
            CurrentID = new ReusableID(HighestPersistentID+1, 0);
            foreach (var id in GetNonPersistentIDs())
            {
                if (id < CurrentID)
                    ReturnID(id);
                IntToID.Remove(id);
                GlobalIDToID.Remove(id);
                Holders.Remove(id);
            }
            //ReusableIDs.Clear();
            //IntToID.Clear();
            //GlobalIDToID.Clear();
            //Holders.Clear();
        }
    }

    static class EffectManager
    {
        class EffectList : List<Effect>
        {
            public int Generation;
            public bool Persist;

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

            public void Clear()
            {
                for(int i = 0; i < Lists.Length; i++)
                {
                    if (!Lists[i]?.Persist ?? false)
                        Lists[i] = null;
                }
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
                return Effects.Capacity > holder.ObjectID && (list = Effects[holder.ObjectID]) != null && holder.ObjectID.Generation == list.Generation && holder.ObjectID.Valid;
            }

            public void Add(IEffectHolder holder, Effect effect)
            {
                if (holder.ObjectID == ReusableID.Null)
                    return;
                if (Has(holder))
                    Effects[holder.ObjectID].Add(effect);
                else
                {
                    if (Effects.Capacity > holder.ObjectID && Effects[holder.ObjectID] != null)
                    {
                        var effects = Effects[holder.ObjectID];
                        for(int i = effects.Count; i >= 0; i--)
                        {
                            if (i < effects.Count)
                                effects[i].Remove();
                        }
                    }
                    Effects[holder.ObjectID] = new EffectList(holder.ObjectID.Generation) { effect };
                }
                Effects[holder.ObjectID].Persist = holder is IEffectHolderPersistent;
            }

            public void Remove(IEffectHolder holder, Effect effect)
            {
                if (holder.ObjectID == ReusableID.Null)
                    return;
                if (Has(holder))
                    Effects[holder.ObjectID].Remove(effect);
            }

            public void Clear()
            {
                Effects.Clear();
            }
        }

        
        static Dictionary<Type, Drawer> Drawers = new Dictionary<Type, Drawer>();
        static EffectHolderRegistry Holders = new EffectHolderRegistry();

        static EffectManager()
        {
        }

        public static IEnumerable<T> GetEffects<T>(IEffectHolder holder, bool split = true) where T : Effect
        {
            if (holder.ObjectID == ReusableID.Null)
                return Enumerable.Empty<T>();
            var effects = GetDrawer(typeof(Effect)).Get(holder).Where(x => !x.Type.HasFlag(EffectType.NoApply));
            if (split)
                return effects.SplitEffects<T>();
            else
                return effects.OfType<T>();
        }

        public static IEnumerable<T> SplitEffects<T>(this IEnumerable<Effect> effects) where T : Effect
        {
            foreach(var effect in effects)
            {
                if (effect is T)
                    yield return (T)effect;
                if (effect is IEffectContainer container)
                    foreach (var contained in container.GetSubEffects<T>())
                        yield return contained;
            }
        }

        private static Drawer GetDrawer(Type type)
        {
            if (!Drawers.ContainsKey(type))
                Drawers.Add(type, new Drawer());
            return Drawers[type];
        }

        public static void Reset()
        {
            GetDrawer(typeof(Effect)).Clear();
            Holders.Clear();
        }

        public static void AddEffect(this IEffectHolder holder, Effect effect)
        {
            GetDrawer(typeof(Effect)).Add(holder, effect);
        }

        public static void RemoveEffect(this IEffectHolder holder, Effect effect)
        {
            GetDrawer(typeof(Effect)).Remove(holder, effect);
        }

        public static void ClearEffects(this IEffectHolder holder)
        {
            foreach (var effect in GetEffects<Effect>(holder, false))
                effect.Remove();
        }

        public static double GetDamage(this IEffectHolder holder, Element element)
        {
            return holder.GetEffects<EffectDamage>().Where(x => x.Element == element).Sum(x => x.Amount);
        }

        public static double GetTotalDamage(this IEffectHolder holder)
        {
            return holder.GetEffects<EffectDamage>().Sum(x => x.Amount);
        }

        public static double GetStat(this IEffectHolder holder, Stat stat)
        {
            return CalculateStat(holder,holder.GetEffects<Effect>().Where(effect => effect is IStat statEffect && statEffect.Stat == stat),stat.DefaultStat);
        }

        public static double GetStatDamage(this IEffectHolder holder, Stat stat)
        {
            return holder.GetEffects<EffectStatDamage>().Where(x => x.Stat == stat).Sum(x => x.Amount);
        }

        public static Dictionary<Stat,double> GetStats(this IEffectHolder holder)
        {
            return holder.GetEffects<Effect>().OfType<IStat>().GroupBy(stat => stat.Stat, stat => (Effect)stat).ToDictionary(group => group.Key, group => CalculateStat(holder, group, group.Key.DefaultStat));
        }

        public static Dictionary<Element, double> GetElements(this IEffectHolder holder)
        {
            return holder.GetEffects<EffectElement>().GroupBy(stat => stat.Element, stat => stat).ToDictionary(group => group.Key, group => group.Sum(element => element.Percentage));
        }

        public static int GetTrait(this IEffectHolder holder, Trait trait)
        {
            var effects = holder.GetEffects<EffectTrait>().Where(effect => effect.Trait == trait);
            return Math.Max(0,effects.Sum(x => x.Level));
        }

        public static double CalculateStat(IEffectHolder holder, IEnumerable<Effect> effects, double defaultStat)
        {
            var groups = effects.ToTypeLookup();
            var baseStat = defaultStat + groups.Get<EffectStat>().Where(stat => stat.Base).Sum(stat => stat.Amount(holder));
            var add = groups.Get<EffectStat>().Where(stat => !stat.Base).Sum(stat => stat.Amount(holder));
            var percentage = groups.Get<EffectStatPercent>().Sum(stat => stat.Percentage);
            var multiplier = groups.Get<EffectStatMultiply>().Aggregate(1.0, (seed, stat) => seed * stat.Multiplier);
            var locks = groups.Get<EffectStatLock>();
            var min = locks.Any() ? locks.Max(stat => stat.MinValue) : double.NegativeInfinity;
            var max = locks.Any() ? locks.Min(stat => stat.MaxValue) : double.PositiveInfinity;
            var damage = groups.Get<EffectStatDamage>().Sum(stat => stat.Amount);

            return Math.Max(min, Math.Min((baseStat + percentage * baseStat + add) * multiplier - damage, max));
        }

        /*public static string GetStatBonus(this IEnumerable<Effect> effects, Stat statName)
        {
            string statBlock = string.Empty;
            var groups = effects.ToTypeLookup();
            var baseStat = groups.Get<EffectStat>().Where(stat => stat.Base).Sum(stat => stat.Amount);
            if (baseStat != 0)
                statBlock += $"{Game.FormatStat(statName)} {statName.Name} {baseStat.ToString("+0;-#")} Base\n";
            var add = groups.Get<EffectStat>().Where(stat => !stat.Base).Sum(stat => stat.Amount);
            if (add != 0)
                statBlock += $"{Game.FormatStat(statName)} {statName.Name} {add.ToString("+0;-#")}\n";
            var percentage = groups.Get<EffectStatPercent>().Sum(stat => stat.Percentage);
            if (percentage != 0)
                statBlock += $"{Game.FormatStat(statName)} {statName.Name} {((int)Math.Round(percentage * 100)).ToString("+0;-#")}%\n";
            var multiplier = groups.Get<EffectStatMultiply>().Aggregate(1.0, (seed, stat) => seed * stat.Multiplier);
            if (multiplier != 1)
                statBlock += $"{Game.FormatStat(statName)} {statName.Name} x{Math.Round(multiplier,2)}\n";
            var damage = groups.Get<EffectStatDamage>().Sum(stat => stat.Amount);
            var locks = groups.Get<EffectStatLock>();
            var min = locks.Any() ? locks.Max(stat => stat.MinValue) : double.NegativeInfinity;
            var max = locks.Any() ? locks.Min(stat => stat.MaxValue) : double.PositiveInfinity;

            return statBlock;
        }*/

        public static void TakeDamage(this IEffectHolder holder, double damage, Element element)
        {
            TakeDamage(holder, damage, element, PopupHelper.Global);
        }

        public static void TakeDamage(this IEffectHolder holder, double damage, Element element, PopupHelper popupHelper)
        {
            popupHelper?.Add(new MessageDamage(holder, damage, element));
            Effect.Apply(new EffectDamage(holder, damage, element));
        }

        public static void TakeStatDamage(this IEffectHolder holder, double damage, Stat stat)
        {
            //PopupManager.Add(new MessageStatDamage(holder, damage, stat));
            Effect.Apply(new EffectStatDamage(holder, damage, stat));
        }

        public static void Heal(this IEffectHolder holder, double heal)
        {
            Heal(holder, heal, PopupHelper.Global);
        }

        public static void Heal(this IEffectHolder holder, double heal, PopupHelper popupHelper)
        {
            var damageEffects = holder.GetEffects<EffectDamage>().ToList();
            var totalDamage = damageEffects.Sum(x => x.Amount);
            var totalHeal = heal + Math.Max(0, totalDamage - holder.GetStat(Stat.HP));
            var healAmounts = Util.ProportionalSplit(damageEffects.Select(x => x.Amount), totalHeal);
            int i = 0;
            foreach(var damage in damageEffects)
            {
                damage.Amount -= healAmounts[i];
                if (damage.Amount <= 0)
                    damage.Remove();
                i++;
            }

            popupHelper?.Add(new MessageHeal(holder, heal));
        }

        public static Wait OnStartDefend(this IEffectHolder holder, Attack attack)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<Attack, OnStartDefend>(attack));
        }

        public static Wait OnDefend(this IEffectHolder holder, Attack attack)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<Attack, OnDefend>(attack));
        }

        public static Wait OnStartMine(this IEffectHolder holder, MineEvent mine)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<MineEvent, OnStartMine>(mine));
        }

        public static Wait OnMine(this IEffectHolder holder, MineEvent mine)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<MineEvent,OnMine>(mine));
        }

        public static Wait OnDeath(this IEffectHolder holder, DeathEvent death)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<DeathEvent, OnDeath>(death));
        }

        public static Wait OnTurn(this IEffectHolder holder, TurnEvent turn)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<TurnEvent, OnTurn>(turn));
        }

        public static Wait OnShoot(this IEffectHolder holder, ShootEvent shoot)
        {
            return Scheduler.Instance.RunAndWait(holder.PushEvent<ShootEvent, OnShoot>(shoot));
        }

        public static IEnumerable<Wait> PushEvent<T,V>(this IEffectHolder holder, T eventParam) where V : EffectEvent<T>
        {
            foreach (var effect in holder.GetEffects<V>().Distinct())
            {
                yield return effect.Trigger(eventParam);
            }
        }

        public static IEnumerable<Item> GetInventory(this IEffectHolder holder)
        {
            return holder.GetEffects<EffectItemInventory>().Select(effect => effect.Item);
        }

        public static StatusEffect GetStatusEffect(this IEffectHolder holder, StatusEffect statusEffect)
        {
            foreach (var checkEffect in holder.GetEffects<EffectStatusEffect>())
            {
                if (checkEffect.StatusEffect == statusEffect || checkEffect.StatusEffect.CanCombine(statusEffect))
                    return checkEffect.StatusEffect;
            }
            return null;
        }

        public static T GetStatusEffect<T>(this IEffectHolder holder) where T : StatusEffect
        {
            foreach(var checkEffect in holder.GetEffects<EffectStatusEffect>())
            {
                if (checkEffect.StatusEffect is T)
                    return (T)checkEffect.StatusEffect;
            }
            return null;
        }

        public static bool HasStatusEffect(this IEffectHolder holder, Func<StatusEffect,bool> match)
        {
            return holder.GetStatusEffects().Any(match);
        }

        public static bool HasStatusEffect<T>(this IEffectHolder holder) where T : StatusEffect
        {
            return holder.GetStatusEffects().Any(statusEffect => statusEffect is T);
        }

        public static IEnumerable<StatusEffect> GetStatusEffects(this IEffectHolder holder)
        {
            return holder.GetEffects<EffectStatusEffect>().Select(x => x.StatusEffect);
        }

        public static void ClearStatusEffects(this IEffectHolder holder)
        {
            foreach (var effect in holder.GetEffects<EffectStatusEffect>())
                effect.Remove();
        }

        public static void ClearStatusEffects(this IEffectHolder holder, Func<StatusEffect, bool> match)
        {
            foreach (var effect in holder.GetEffects<EffectStatusEffect>().Where(effect => match(effect.StatusEffect)))
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



        public static int GetStatusStacks<T>(this IEffectHolder holder) where T : StatusEffect
        {
            T statusEffect = holder.GetStatusEffect<T>();
            return statusEffect?.Stacks ?? 0;
        }

        public static void AddName(this IEffectHolder holder, string name)
        {
            var names = holder.GetEffects<EffectName>();
            double priority = 0;
            if (names.Any())
            {
                priority = names.Max(x => x.Priority) + 1;
            }
            Effect.Apply(new EffectName(holder, name, priority));
        }

        public static string GetName(this IEffectHolder holder, string defaultName)
        {
            var effects = holder.GetEffects<EffectName>();
            if (effects.Any())
                return effects.WithMax(effect => effect.Priority).Name;
            return defaultName;
        }

        public static bool HasFlag(this IEffectHolder holder, Flag flag)
        {
            var effects = holder.GetEffects<EffectFlag>().Where(effect => effect.Flag == flag);
            if (effects.Any())
                return effects.WithMax(effect => effect.Priority).Value;
            return flag.DefaultValue;
        }

        public static bool HasFamily(this IEffectHolder holder, Family family)
        {
            var effects = holder.GetEffects<EffectFamily>().Where(effect => effect.Family == family);
            if (effects.Any())
                return effects.WithMax(effect => effect.Priority).Value;
            return false;
        }

        public static IEnumerable<Family> GetFamilies(this IEffectHolder holder)
        {
            var effectGroups = holder.GetEffects<EffectFamily>().GroupBy(effect => effect.Family);
            return effectGroups.Select(group => group.WithMax(effect => effect.Priority)).Where(effect => effect.Value).Select(effect => effect.Family);
        }

        public static void ClearPosition(this IEffectHolder subject)
        {
            foreach (var position in subject.GetEffects<Effect>().Where(x => x is IPosition position && position.Subject == subject))
            {
                position.Remove();
            }
        }

        public static ReusableID SetID(IEffectHolder holder)
        {
            return Holders.SetID(holder);
        }

        public static Guid SetGlobalID(IEffectHolder holder)
        {
            return Holders.SetGlobalID(holder);
        }

        public static Guid SetGlobalID(IEffectHolder holder, Guid globalId)
        {
            return Holders.SetGlobalID(holder, globalId);
        }

        public static bool HasHolder(int id)
        {
            return Holders.Has(id);
        }

        public static IEffectHolder GetHolder(int id)
        {
            return Holders.Get(id);
        }

        public static IEffectHolder GetHolder(Guid globalId)
        {
            return Holders.Get(globalId);
        }

        public static void DeleteHolder(IEffectHolder holder)
        {
            Holders.Remove(holder);
        }
    }
}
