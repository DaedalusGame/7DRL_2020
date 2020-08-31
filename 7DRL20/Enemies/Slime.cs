using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Skills;
using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Enemies
{
    class AcidBlob : Enemy
    {
        public AcidBlob(SceneGame world) : base(world)
        {
            Name = "Acid Blob";
            Description = "I'm the trashman";

            Render = new CreatureBlobRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/blob_acid")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 120));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 15));

            Effect.ApplyInnate(new EffectFamily(this, Family.Slime));

            Skills.Add(new SkillAcidTouch());
            Skills.Add(new SkillAttack());
        }

        [Construct("blob_acid")]
        public static AcidBlob Construct(Context context)
        {
            return new AcidBlob(context.World);
        }
    }

    class PoisonBlob : Enemy
    {
        public PoisonBlob(SceneGame world) : base(world)
        {
            Name = "Poison Blob";
            Description = "How dare you";

            Render = new CreatureBlobRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/blob_poison")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 120));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 15));

            Effect.ApplyInnate(new EffectFamily(this, Family.Slime));

            Skills.Add(new SkillPoisonTouch());
            Skills.Add(new SkillAttack());
        }

        [Construct("blob_poison")]
        public static PoisonBlob Construct(Context context)
        {
            return new PoisonBlob(context.World);
        }
    }

    class GreenBlob : Enemy
    {
        public GreenBlob(SceneGame world) : base(world)
        {
            Name = "Green Blob";
            Description = "Forgive and forget";

            Render = new CreatureBlobRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/blob_green")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectFamily(this, Family.Slime));
            Effect.ApplyInnate(new EffectFamily(this, Family.GreenSlime));

            Effect.ApplyInnate(new EffectTrait(this, Trait.SplitGreenSlime));

            Skills.Add(new SkillSlimeTouch());
            Skills.Add(new SkillAttack());
        }

        public GreenBlob(SceneGame world, double hp) : this(world)
        {
            Effect.Apply(new EffectStat(this, Stat.HP, hp));
        }

        [Construct("blob_green")]
        public static GreenBlob Construct(Context context)
        {
            return new GreenBlob(context.World);
        }

        public override bool IsHostile(Creature other)
        {
            return !other.HasFamily(Family.GreenSlime);
        }
    }

    class GreenAmoeba : Enemy
    {
        public GreenAmoeba(SceneGame world) : base(world)
        {
            Name = "Green Amoeba";
            Description = "I'm baby";

            Render = new CreatureBlobRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/amoeba_green")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 15));

            Effect.ApplyInnate(new EffectFamily(this, Family.Slime));
            Effect.ApplyInnate(new EffectFamily(this, Family.GreenSlime));

            Skills.Add(new SkillSlimeTouch());
        }

        public GreenAmoeba(SceneGame world, double hp) : this(world)
        {
            Effect.Apply(new EffectStat(this, Stat.HP, hp));
        }

        [Construct("amoeba_green")]
        public static GreenAmoeba Construct(Context context)
        {
            return new GreenAmoeba(context.World);
        }

        public override bool IsHostile(Creature other)
        {
            return !other.HasFamily(Family.GreenSlime);
        }
    }
}
