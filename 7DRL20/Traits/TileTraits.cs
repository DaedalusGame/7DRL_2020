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

        public TraitWater() : base("on_water", "On Water", "Become wet every turn.", new Color(16, 16, 255))
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

        public TraitLava() : base("on_lava", "On Lava", 
            $"Take {Game.FormatElement(Element.Fire)}{Element.Fire.Name} damage and become Aflame every turn.", new Color(255, 128, 16))
        {
            Effect.Apply(new OnTurn(this, OnLava));
        }

        private IEnumerable<Wait> OnLava(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            yield return creature.AttackSelf(LavaAttack);
        }

        private Attack LavaAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(10, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            attack.StatusEffects.Add(new Aflame()
            {
                Buildup = 1,
                Duration = new Slider(20),
            });
            return attack;
        }
    }

    class TraitSuperLava : Trait
    {
        Random Random = new Random();

        public TraitSuperLava() : base("on_super_lava", "On Super Lava", 
            $"{Game.FormatStat(Element.Fire.DamageRate)}{Element.Fire.DamageRate.Name} +50%.\n" +
            $"Melts armor and weapons every turn.", new Color(255, 192, 32))
        {
            Effect.Apply(new OnTurn(this, OnLava));
            Effect.Apply(new EffectStatPercent(this, Element.Fire.DamageRate, 0.5));
        }

        private IEnumerable<Wait> OnLava(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            yield return creature.AttackSelf(LavaAttack);
        }

        private Attack LavaAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(20, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            attack.StatusEffects.Add(new Aflame()
            {
                Buildup = 1,
                Duration = new Slider(20),
            });
            return attack;
        }
    }

    class TraitHyperLava : Trait
    {
        Random Random = new Random();

        public TraitHyperLava() : base("on_hyper_lava", "On Hyper Lava", 
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

            yield return creature.AttackSelf(LavaAttack);
        }

        private Attack LavaAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
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
            return attack;
        }
    }

    class TraitAcid : Trait
    {
        Random Random = new Random();

        public TraitAcid() : base("on_acid", "On Acid", "Take acid damage every turn.", new Color(128, 255, 16))
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

    class TraitBog : Trait
    {
        Random Random = new Random();

        public TraitBog() : base("on_bog", "On Bog",
            $"Become muddied every turn.", new Color(133, 121, 92))
        {
            Effect.Apply(new OnTurn(this, OnBog));
        }

        private IEnumerable<Wait> OnBog(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            creature.AddStatusEffect(new Muddy()
            {
                Buildup = 1.0,
                Duration = new Slider(20),
            });

            yield return Wait.NoWait;

            //yield return creature.AttackSelf(BogAttack);
        }

        private Attack BogAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.StatusEffects.Add(new Muddy()
            {
                Buildup = 1,
                Duration = new Slider(20),
            });
            return attack;
        }
    }
}
