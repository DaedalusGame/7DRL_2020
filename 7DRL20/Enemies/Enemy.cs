using Microsoft.Xna.Framework;
using RoguelikeEngine.Attacks;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Skills;
using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Enemies
{
    class Family
    {
        public static List<Family> AllFamilies = new List<Family>();

        int Index;
        public string ID;
        public string Name;

        public Family(string id, string name)
        {
            Index = AllFamilies.Count;
            ID = id;
            Name = name;
            AllFamilies.Add(this);
        }

        public static Family GetFamily(string id)
        {
            return AllFamilies.Find(x => x.ID == id);
        }

        public static Family Boss = new Family("boss", "Extinction Unit");

        public static Family Bloodless = new Family("bloodless", "Bloodless");
        public static Family Fish = new Family("fish", "Fish");
        public static Family Undead = new Family("undead", "Undead");
        public static Family Dragon = new Family("dragon", "Dragon");
        public static Family Slime = new Family("slime", "Slime");
        public static Family Construct = new Family("construct", "Construct");
        public static Family Plant = new Family("plant", "Plant");
        public static Family Abyssal = new Family("abyssal", "Abyssal");

        public static Family GreenSlime = new Family("slime_green", "Green Slime");
    }

    class MovementType
    {
        public static List<MovementType> AllMovementTypes = new List<MovementType>();

        int Index;
        public string ID;

        public Func<IEnumerable<Point>> GetNeighbors;
        public Func<Tile, double> GetTileCost;
        public Func<Tile, bool> CanTraverse;

        public MovementType(string id)
        {
            Index = AllMovementTypes.Count;
            ID = id;
            AllMovementTypes.Add(this);
        }

        private static IEnumerable<Point> GetEmptyNeighbors()
        {
            return Enumerable.Empty<Point>();
        }

        private static IEnumerable<Point> GetCardinalNeighbors()
        {
            yield return new Point(0, -1);
            yield return new Point(0, +1);
            yield return new Point(-1, 0);
            yield return new Point(+1, 0);
        }

        private static IEnumerable<Point> GetDiagonalNeighbors()
        {
            yield return new Point(-1, -1);
            yield return new Point(-1, +1);
            yield return new Point(+1, -1);
            yield return new Point(+1, +1);
        }

        private static IEnumerable<Point> GetAllNeighbors()
        {
            return GetCardinalNeighbors().Concat(GetDiagonalNeighbors());
        }

        public static MovementType GetMovementType(string id)
        {
            return AllMovementTypes.Find(x => x.ID == id);
        }

        public static double StandardCost(Tile tile)
        {
            if (tile.Solid)
                return 1000;
            return 1;
        }

        public static double WaterOnlyCost(Tile tile)
        {
            if (tile is Water)
                return 1;
            return 1000;
        }

        public static bool StandardTraverse(Tile tile)
        {
            return !tile.Solid;
        }

        public static bool WaterOnlyTraverse(Tile tile)
        {
            return tile is Water;
        }

        public static MovementType Standard = new MovementType("standard")
        {
            GetNeighbors = GetCardinalNeighbors,
            GetTileCost = StandardCost,
            CanTraverse = StandardTraverse,
        };
        public static MovementType Stationary = new MovementType("stationary")
        {
            GetNeighbors = GetEmptyNeighbors,
        };
        public static MovementType Diagonal = new MovementType("diagonal")
        {
            GetNeighbors = GetDiagonalNeighbors,
        };
        public static MovementType AllDirections = new MovementType("all_directions")
        {
            GetNeighbors = GetAllNeighbors,
        };
        public static MovementType WaterOnly = new MovementType("water_only")
        {
            GetTileCost = WaterOnlyCost,
            CanTraverse = WaterOnlyTraverse,
        };
    }

    abstract class Enemy : Creature
    {
        static public Random Random = new Random();
        public Creature AggroTarget;

        public virtual string BossMessage => "WARNING\n\nMANIFESTATION OF EXTINCTION UNIT";
        public virtual string[] BossDescription => new string[] { BossMessage };

        public List<Skill> Skills = new List<Skill>();

        public Enemy(SceneGame world) : base(world)
        {
            Experience = 100;
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

        public double GetWeightStraight(Point start, Point end)
        {
            var tiles = Mask.Select(o => Map.GetTile(end.X + o.X, end.Y + o.Y));
            return tiles.Select(GetTileWeight).Sum();
        }

        private bool CanEnter(Tile tile)
        {
            return !tile.Solid;
        }

        private double GetTileWeight(Tile tile)
        {
            if (!CanEnter(tile))
                return 100;
            else if(tile.Creatures.Any())
                return 10;
            else
                return 1;
        }

        public IEnumerable<Point> GetNeighbors(Point point)
        {
            yield return new Point(point.X, point.Y - 1);
            yield return new Point(point.X, point.Y + 1);
            yield return new Point(point.X - 1, point.Y);
            yield return new Point(point.X + 1, point.Y);
        }

        public override Wait TakeTurn(Turn turn)
        {
            if (Dead)
                return Wait.NoWait;
            
            return Scheduler.Instance.RunAndWait(RoutineTakeTurn(turn));
        }

        private IEnumerable<Wait> RoutineTakeTurn(Turn turn)
        {
            FindAggroTarget();
            if (AggroTarget != null)
                FaceTowards(AggroTarget);
            Skill usableSkill = GetUsableSkill();
            foreach (Skill skill in Skills)
                skill.Update(this);

            if (usableSkill != null)
            {
                var target = usableSkill.GetEnemyTarget(this);
                CurrentActions.Add(Scheduler.Instance.RunAndWait(RoutineUseSkill(usableSkill, target)));
                if (usableSkill.WaitUse)
                    yield return CurrentActions;
            }
            else
            {
                var move = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Pick(Random).ToOffset();
                if (AggroTarget != null)
                {
                    var movementType = this.GetMovementType();
                    var costMap = new CostMap(Map, this, movementType.GetTileCost);
                    costMap.SetMask(Mask);
                    costMap.Recalculate();
                    var targetPoints = Mask.Select(o => new Point(AggroTarget.X, AggroTarget.Y) - o).ToList();
                    var dijkstra = Util.Dijkstra(new Point[] { new Point(X, Y) }, targetPoints, Map.Width, Map.Height, new Rectangle(X - 20, Y - 20, 41, 41), double.MaxValue, costMap, movementType.GetNeighbors());
                    var path = dijkstra.FindPath(targetPoints.Pick(Random));
                    if (path.Any())
                    {
                        move = path.First() - new Point(X, Y);
                    }
                    else
                    {
                        move = new Point(0, 0);
                    }
                }

                if (move != Point.Zero) //No move to self
                    CurrentActions.Add(Scheduler.Instance.RunAndWait(RoutineMove(move.X, move.Y)));
            }
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

        public IEnumerable<Wait> RoutineUseSkill(Skill skill, object target)
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
        public SpriteReference Glow;
        public ColorMatrix Color = ColorMatrix.Identity;
        public ColorMatrix GlowColor = ColorMatrix.Identity;

        public Func<float> GlowAmount;

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            var mirror = GetMirror(facing);
            int facingOffset = GetFacingOffset(facing);
            int frameOffset = GetFrameOffset(poseData);

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(Sprite, facingOffset + frameOffset, pos, mirror, color, 0);
            scene.PopSpriteBatch();
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(GlowColor, transform * matrix, projection);
            });
            if (Glow != null && GlowAmount != null)
                scene.DrawSprite(Glow, facingOffset + frameOffset, pos, mirror, Microsoft.Xna.Framework.Color.White * GlowAmount(), 0);
            scene.PopSpriteBatch();
        }

        protected static Microsoft.Xna.Framework.Graphics.SpriteEffects GetMirror(Facing facing)
        {
            return facing == Facing.East ? Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally : Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
        }

        protected static int GetFacingOffset(Facing facing)
        {
            int facingOffset = 0;
            switch (facing)
            {
                case (Facing.North):
                    facingOffset = 8;
                    break;
                case (Facing.East):
                    facingOffset = 4;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 4;
                    break;
            }

            return facingOffset;
        }

        protected static int GetFrameOffset(PoseData poseData)
        {
            int frameOffset = 0;

            switch (poseData.Pose)
            {
                case (CreaturePose.Stand):
                    frameOffset = 1;
                    break;
                case (CreaturePose.Walk):
                    double lerp = LerpHelper.ForwardReverse(0, 2, (poseData.Frame / 50.0) % 1);
                    frameOffset = (int)Math.Round(lerp);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 3;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 3;
                    break;
            }

            return frameOffset;
        }
    }

    class CreatureDirectionalRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public CreatureDirectionalRender()
        {
        }

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (facing)
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
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(Sprite, facingOffset, pos, mirror, color, 0);
            scene.PopSpriteBatch();
        }
    }

    class CreatureBlobRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int frameOffset = 0;
            switch (poseData.Pose)
            {
                case (CreaturePose.Stand):
                    frameOffset = (int)Math.Round(poseData.Frame / 40.0);
                    break;
                case (CreaturePose.Walk):
                    frameOffset = (int)Math.Round(poseData.Frame / 10.0);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = (int)Math.Round(poseData.Frame / 5.0);
                    break;
                case (CreaturePose.Cast):
                    frameOffset = (int)Math.Round(poseData.Frame / 3.0);
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(Sprite, frameOffset, pos, mirror, color, 0);
            scene.PopSpriteBatch();
        }
    }

    class BigCreatureRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (facing)
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
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(Sprite, facingOffset, pos, mirror, color, 0);
            scene.PopSpriteBatch();
        }
    }

    class MardukeRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            var mirror = GetMirror(facing);
            int facingOffset = GetFacingOffset(facing);
            int frameOffset = GetFrameOffset(poseData);

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(Sprite, facingOffset + frameOffset, pos, mirror, color, 0);
            scene.PopSpriteBatch();
        }

        protected static Microsoft.Xna.Framework.Graphics.SpriteEffects GetMirror(Facing facing)
        {
            return facing == Facing.East ? Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally : Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
        }

        protected static int GetFacingOffset(Facing facing)
        {
            int facingOffset = 0;
            switch (facing)
            {
                case (Facing.North):
                    facingOffset = 0;
                    break;
                case (Facing.East):
                    facingOffset = 5;
                    break;
                case (Facing.South):
                    facingOffset = 10;
                    break;
                case (Facing.West):
                    facingOffset = 5;
                    break;
            }

            return facingOffset;
        }

        protected static int GetFrameOffset(PoseData poseData)
        {
            int frameOffset = 0;

            switch (poseData.Pose)
            {
                case (CreaturePose.Stand):
                    frameOffset = 1;
                    break;
                case (CreaturePose.Walk):
                    double lerp = LerpHelper.ForwardReverse(0, 2, (poseData.Frame / 50.0) % 1);
                    frameOffset = (int)Math.Round(lerp);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 3;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 4;
                    break;
            }

            return frameOffset;
        }
    }

    class CreatureStaticRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public float AnimationSpeedStand = 1 / 6f;
        public float AnimationSpeedWalk = 1 / 4f;
        public float AnimationSpeedAttack = 0;
        public float AnimationSpeedCast = 0;

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;

            int frameOffset = 0;
            switch (poseData.Pose)
            {
                case (CreaturePose.Stand):
                    frameOffset = (int)Math.Round(poseData.Frame * AnimationSpeedStand);
                    break;
                case (CreaturePose.Walk):
                    frameOffset = (int)Math.Round(poseData.Frame * AnimationSpeedWalk);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = (int)Math.Round(poseData.Frame * AnimationSpeedAttack);
                    break;
                case (CreaturePose.Cast):
                    frameOffset = (int)Math.Round(poseData.Frame * AnimationSpeedCast);
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(Sprite, frameOffset, pos, mirror, color, 0);
            scene.PopSpriteBatch();
        }
    }

    class BlastCannon : Enemy
    {
        public BlastCannon(SceneGame world) : base(world)
        {
            Name = "Blast Cannon";
            Description = "High Sentry";

            Render = new CreatureDirectionalRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/cannon"),
                Color = ColorMatrix.TwoColor(Color.Black, Color.LightSeaGreen)
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 600));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 25));

            Effect.ApplyInnate(new EffectMovementType(this, MovementType.Stationary, 10));

            Skills.Add(new SkillCannonShot());
        }

        [Construct("blast_cannon")]
        public static BlastCannon Construct(Context context)
        {
            return new BlastCannon(context.World);
        }
    }

    class SwampHag : Enemy
    {
        public SwampHag(SceneGame world) : base(world)
        {
            Name = "Swamp Hag";
            Description = "A nice grandma to bake cookies from your entrails";

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_hag"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_robe"),
                BodyColor = ColorMatrix.TwoColor(new Color(95, 61, 92), new Color(107, 139, 86)),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 250));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 30));

            Skills.Add(new SkillHagsKnife());
            Skills.Add(new SkillBoilTallow());
            Skills.Add(new SkillTallowCurse());
            Skills.Add(new SkillRaisePeatMummy());
            Skills.Add(new SkillIgniteBog());
        }

        [Construct("hag_swamp")]
        public static SwampHag Construct(Context context)
        {
            return new SwampHag(context.World);
        }
    }

    class WalkingCauldron : Enemy
    {
        public WalkingCauldron(SceneGame world) : base(world)
        {
            Name = "Walking Cauldron";
            Description = "Humpty Dumpty";

            Render = new CreatureStaticRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/walking_cauldron"),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Fire.DamageRate, -0.5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Broiling));
        }

        [Construct("walking_cauldron")]
        public static WalkingCauldron Construct(Context context)
        {
            return new WalkingCauldron(context.World);
        }
    }

    class AutoBomb : Enemy
    {
        public AutoBomb(SceneGame world) : base(world)
        {
            Name = "Auto Bomb";
            Description = "Does not enable wall clips.";

            Render = new CreatureStaticRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/mine"),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 150));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Fire.DamageRate, -0.5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.DeathThroesFireBlast));

            Skills.Add(new SkillKamikaze());
        }

        [Construct("auto_bomb")]
        public static AutoBomb Construct(Context context)
        {
            return new AutoBomb(context.World);
        }
    }
}
