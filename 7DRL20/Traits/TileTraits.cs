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

        public TraitLava() : base("On Lava", 
            $"Take {Game.FormatElement(Element.Fire)}{Element.Fire.Name} damage and become Aflame every turn.", new Color(255, 128, 16))
        {
            Effect.Apply(new OnTurn(this, OnLava));
        }

        private IEnumerable<Wait> OnLava(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            Attack attack = new Attack(creature, creature);
            attack.SetParameters(10, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            attack.StatusEffects.Add(new Aflame()
            {
                Buildup = 1,
                Duration = new Slider(20),
            });

            yield return creature.AttackSelf(attack);
        }
    }

    class TraitSuperLava : Trait
    {
        Random Random = new Random();

        public TraitSuperLava() : base("On Super Lava", 
            $"{Game.FormatStat(Element.Fire.DamageRate)}{Element.Fire.DamageRate.Name} +50%.\n" +
            $"Melts armor and weapons every turn.", new Color(255, 192, 32))
        {
            Effect.Apply(new OnTurn(this, OnLava));
            Effect.Apply(new EffectStatPercent(this, Element.Fire.DamageRate, 0.5));
        }

        private IEnumerable<Wait> OnLava(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            Attack attack = new Attack(creature, creature);
            attack.SetParameters(20, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            attack.StatusEffects.Add(new Aflame()
            {
                Buildup = 1,
                Duration = new Slider(20),
            });

            yield return creature.AttackSelf(attack);
        }
    }

    class TraitHyperLava : Trait
    {
        Random Random = new Random();

        public TraitHyperLava() : base("On Hyper Lava", 
            $"Take {Game.FormatElement(Element.Magma)}{Element.Magma.Name} damage every turn.\n" +
            $"{Game.FormatStat(Element.Fire.DamageRate)}{Element.Fire.DamageRate.Name} +100%.\n" +
            $"Build Incinerate every turn.", new Color(255, 255, 64))
        {
            Effect.Apply(new OnTurn(this, OnLava));
            Effect.Apply(new EffectStatPercent(this, Element.Fire.DamageRate, 1.0));
        }

        private IEnumerable<Wait> OnLava(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            Attack attack = new Attack(creature, creature);
            attack.SetParameters(40, 0, 1);
            attack.Elements.Add(Element.Magma, 1);
            attack.StatusEffects.Add(new Aflame()
            {
                Buildup = 1,
                Duration = new Slider(20),
            });
            attack.StatusEffects.Add(new Incinerate()
            {
                Buildup = 0.2,
                Duration = new Slider(15),
            });

            yield return creature.AttackSelf(attack);
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
            creature.CheckDead(Vector2.Zero);

            yield return Wait.NoWait;
        }
    }
}
