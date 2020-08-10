using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Traits
{
    class TraitWater : Trait
    {
        Random Random = new Random();

        public TraitWater() : base("On Water", "Become wet every turn.", new Color(16, 16, 255))
        {
            Effect.Apply(new OnTurn(this, OnWater));
        }

        private IEnumerable<Wait> OnWater(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            creature.AddStatusEffect(new Wet()
            {
                Buildup = 1.0,
                Duration = new Slider(10),
            });

            yield return Wait.NoWait;
        }
    }

    class TraitLava : Trait
    {
        Random Random = new Random();

        public TraitLava() : base("On Lava", "Take fire damage every turn.", new Color(255, 128, 16))
        {
            Effect.Apply(new OnTurn(this, OnLava));
        }

        private IEnumerable<Wait> OnLava(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            creature.TakeDamage(10, Element.Fire);
            creature.CheckDead(0, 0);

            yield return Wait.NoWait;
        }
    }

    class TraitAcid : Trait
    {
        Random Random = new Random();

        public TraitAcid() : base("On Acid", "Take acid damage every turn.", new Color(128, 255, 16))
        {
            Effect.Apply(new OnTurn(this, OnAcid));
        }

        private IEnumerable<Wait> OnAcid(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            creature.TakeDamage(10, Element.Acid);
            creature.CheckDead(0, 0);

            yield return Wait.NoWait;
        }
    }
}
