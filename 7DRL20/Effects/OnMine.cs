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

        public int ReactionLevel;
        public bool Success;

        public MineEvent(Creature miner, Item pickaxe)
        {
            Miner = miner;
            Pickaxe = pickaxe;
            
        }

        public void Setup(IMineable mineable, double requiredMiningLevel, double speed, Action<Creature> lootFunction)
        {
            Mineable = mineable;
            RequiredMiningLevel = requiredMiningLevel;
            Speed = speed;
            LootFunction = lootFunction;
        }

        public void Start()
        {
            double miningLevel = Miner.GetStat(Stat.MiningLevel);

            if (miningLevel >= RequiredMiningLevel)
                Success = true;

            Speed *= Miner.GetStat(Stat.MiningSpeed);

            Miner.OnStartMine(this);

            if (Random.NextDouble() > Speed)
                Success = false;

            Miner.OnMine(this);

            if (Success)
            {
                LootFunction(Miner);
                Mineable.Destroy();
            }
        }
    }

    class OnMine : Effect
    {
        public IEffectHolder Holder;
        public Action<MineEvent> Trigger;

        public OnMine(IEffectHolder holder, Action<MineEvent> trigger)
        {
            Holder = holder;
            Trigger = trigger;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }
    }
}
