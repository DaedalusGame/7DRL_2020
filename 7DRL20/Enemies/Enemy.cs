using Microsoft.Xna.Framework;
using RoguelikeEngine.Attacks;
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
    class Family
    {
        public static List<Family> AllFamilies = new List<Family>();

        int ID;
        public string Name;

        public Family(string name)
        {
            ID = AllFamilies.Count;
            Name = name;
            AllFamilies.Add(this);
        }

        public static Family Boss = new Family("Extinction Unit");

        public static Family Bloodless = new Family("Bloodless");
        public static Family Fish = new Family("Fish");
        public static Family Undead = new Family("Undead");
        public static Family Dragon = new Family("Dragon");
        public static Family Slime = new Family("Slime");

        public static Family GreenSlime = new Family("Green Slime");
    }

    abstract class Enemy : Creature
    {
        static public Random Random = new Random();
        public Creature AggroTarget;

        public virtual string BossMessage => "WARNING\n\nMANIFESTATION OF EXTINCTION UNIT";
        public virtual string[] BossDescription => new string[] { BossMessage };

        protected List<Skill> Skills = new List<Skill>();

        public Enemy(SceneGame world) : base(world)
        {
        }

        public void MakeAggressive(Creature target)
        {
            AggroTarget = target;
        }

        public bool IsSameTeam(Creature other)
        {
            return !IsHostile(other) && !other.IsHostile(this);
        }

        public override bool IsHostile(Creature other)
        {
            return !(other is Enemy);
        }

        private Skill GetUsableSkill()
        {
            IEnumerable<Skill> skills = Skills.Shuffle(Random).OrderByDescending(skill => skill.Priority);
            foreach (Skill skill in skills)
            {
                if (skill.CanEnemyUse(this))
                {
                    return skill;
                }
            }

            return null;
        }

        private void FaceTowards(Rectangle target)
        {
            Rectangle source = Mask.GetRectangle(X,Y);

            int dx = Util.GetDeltaX(source, target);
            int dy = Util.GetDeltaY(source, target);

            Facing? newFacing = Util.GetFacing(dx, dy);
            if (newFacing != null)
                Facing = newFacing.Value;
        }

        private void FaceTowards(Creature target)
        {
            if(target.Tile != null)
                FaceTowards(target.Mask.GetRectangle(target.X,target.Y));
        }

        public override Wait TakeTurn(Turn turn)
        {
            Wait wait = Wait.NoWait;
            if (Dead)
                return wait;
            FindAggroTarget();
            if(AggroTarget != null)
                FaceTowards(AggroTarget);
            Skill usableSkill = GetUsableSkill();
            foreach (Skill skill in Skills)
                skill.Update(this);
            if (usableSkill != null)
            {
                var target = usableSkill.GetEnemyTarget(this);
                CurrentActions.Add(Scheduler.Instance.RunAndWait(RoutineUseSkill(usableSkill, target)));
                wait = usableSkill.WaitUse ? CurrentActions : Wait.NoWait;
            }
            else
            {
                var move = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Pick(Random).ToOffset();
                CurrentActions.Add(Scheduler.Instance.RunAndWait(RoutineMove(move.X, move.Y)));
                //wait = CurrentAction;
            }
            return wait;
        }

        private void FindAggroTarget()
        {
            List<Creature> possibleTargets = new List<Creature>();
            if (AggroTarget != null && AggroTarget.Map != Map)
                AggroTarget = null;
            foreach(var tile in Mask.GetFrontier().Select(o => Tile.GetNeighbor(o.X, o.Y)))
            {
                possibleTargets.AddRange(tile.Creatures.Where(target => !target.Dead && IsHostile(target)));
            }
            if (possibleTargets.Empty())
            {
                possibleTargets.AddRange(Tile.GetNearby(Mask.GetRectangle(X, Y), 8).SelectMany(tile => tile.Creatures).Where(target => !target.Dead && IsHostile(target)));
            }
            if(possibleTargets.Any())
                AggroTarget = possibleTargets.Pick(Random);
        }

        private IEnumerable<Wait> RoutineUseSkill(Skill skill, object target)
        {
            foreach(Wait wait in skill.RoutineUse(this, target))
                yield return wait;
            skill.HideSkill(this);
        }

        public virtual void OnManifest()
        {
            //NOOP
        }

        public override void AddTooltip(ref string tooltip)
        {
            base.AddTooltip(ref tooltip);
            foreach(Skill skill in Skills)
            {
                if(!skill.Hidden(this))
                    tooltip += $"{skill.GetTooltip()}\n";
            }
        }
    }

    class CreatureDragonRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 8;
                    break;
                case (Facing.East):
                    facingOffset = 4;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 4;
                    break;
            }

            int frameOffset = 0;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Stand):
                    frameOffset = 1;
                    break;
                case (CreaturePose.Walk):
                    double lerp = LerpHelper.ForwardReverse(0, 2, (creature.Frame / 50.0) % 1);
                    frameOffset = (int)Math.Round(lerp);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 3;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 3;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(Sprite, facingOffset + frameOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class CreatureFishRender : CreatureRender
    {
        ColorMatrix Color;

        public CreatureFishRender(ColorMatrix color)
        {
            Color = color;
        }

        public override void Draw(SceneGame scene, Creature creature)
        {
            SpriteReference fish = SpriteLoader.Instance.AddSprite("content/fish");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 2;
                    break;
                case (Facing.East):
                    facingOffset = 1;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 1;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(fish, facingOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class CreatureCannonRender : CreatureRender
    {
        ColorMatrix Color;

        public CreatureCannonRender(ColorMatrix color)
        {
            Color = color;
        }

        public override void Draw(SceneGame scene, Creature creature)
        {
            SpriteReference fish = SpriteLoader.Instance.AddSprite("content/cannon");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 2;
                    break;
                case (Facing.East):
                    facingOffset = 1;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 1;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(fish, facingOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class CreatureBlobRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int frameOffset = 0;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Stand):
                    frameOffset = (int)Math.Round(creature.Frame / 40.0);
                    break;
                case (CreaturePose.Walk):
                    frameOffset = (int)Math.Round(creature.Frame / 10.0);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = (int)Math.Round(creature.Frame / 5.0);
                    break;
                case (CreaturePose.Cast):
                    frameOffset = (int)Math.Round(creature.Frame / 3.0);
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(Sprite, frameOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class BigCreatureRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 2;
                    break;
                case (Facing.East):
                    facingOffset = 1;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 1;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(Sprite, facingOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class BlastCannon : Enemy
    {
        public BlastCannon(SceneGame world) : base(world)
        {
            Name = "Blast Cannon";
            Description = "High Sentry";

            Render = new CreatureCannonRender(ColorMatrix.TwoColor(Color.Black, Color.LightSeaGreen));
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 600));
            Effect.Apply(new EffectStat(this, Stat.Attack, 25));

            Effect.Apply(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillCannonShot());
        }
    }
}
