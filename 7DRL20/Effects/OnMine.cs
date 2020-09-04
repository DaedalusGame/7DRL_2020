using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class MineEvent
    {
        public static Random Random = new Random();
        
        public Creature Miner;
        public Item Pickaxe;
        public IMineable Mineable;
        public Action<Creature> LootFunction;

        public double RequiredMiningLevel;
        public double Speed;

        public double Force;

        public IEffectHolder Fault;
        public int ReactionLevel;
        public bool Success;

        List<Wait> Waits = new List<Wait>();

        public MineEvent(Creature miner, Item pickaxe, double force)
        {
            Miner = miner;
            Pickaxe = pickaxe;
            Force = force;
        }

        public void Setup(IMineable mineable, double requiredMiningLevel, double speed, Action<Creature> lootFunction)
        {
            Mineable = mineable;
            RequiredMiningLevel = requiredMiningLevel;
            Speed = speed;
            LootFunction = lootFunction;
        }

        public IEnumerable<Wait> RoutineStart()
        {
            double miningLevel = Miner.GetStat(Stat.MiningLevel);

            if (miningLevel >= RequiredMiningLevel)
                Success = true;

            Speed *= Miner.GetStat(Stat.MiningSpeed);

            yield return Miner.OnStartMine(this);

            if (Success)
            {
                Mineable.Damage += Force * Speed;
                if (Mineable.Damage < Mineable.Durability)
                    Success = false;
            }

            yield return Miner.OnMine(this);

            if (Success)
            {
                LootFunction(Miner);
                Mineable.Destroy();
            }

            yield return new WaitAll(Waits);
        }

        public void AddWait(Wait wait)
        {
            Waits.Add(wait);
        }
    }

    class OnMine : EffectEvent<MineEvent>
    {
        public OnMine(IEffectHolder holder, Func<MineEvent, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }
}
