using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    abstract class SkillBreathBase : Skill
    {
        float StartAngle = -MathHelper.PiOver2;
        float EndAngle = MathHelper.PiOver2;
        float Radius = 4;
        float ArcSpeed = 1;

        public SkillBreathBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 5) && !InRange(user, user.AggroTarget, 2);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.Facing;
        }

        private float GetFacingAngle(Facing facing)
        {
            switch (facing)
            {
                default:
                case (Facing.North): return 0;
                case (Facing.East): return MathHelper.PiOver2;
                case (Facing.South): return MathHelper.Pi;
                case (Facing.West): return MathHelper.Pi + MathHelper.PiOver2;
            }
        }

        protected Tile GetImpactTile(Creature user, float angle, float radius)
        {
            Vector2 direction = Util.AngleToVector(angle);
            Vector2 offset = direction * radius;
            int tileX = (int)(user.VisualTarget.X / 16f + offset.X);
            int tileY = (int)(user.VisualTarget.Y / 16f + offset.Y);
            return user.Tile.Map.GetTile(tileX, tileY);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Facing facing)
            {
                float centerAngle = GetFacingAngle(facing);
                Consume();

                float startAngle = centerAngle - StartAngle;
                float endAngle = centerAngle + EndAngle;
                float radius = Radius;

                float arcLength = (endAngle - startAngle) * radius;
                float arcSpeed = ArcSpeed;

                List<Wait> breaths = new List<Wait>();
                HashSet<Tile> tiles = new HashSet<Tile>();
                PopupManager.StartCollect();
                for (float slide = 0; slide <= arcLength; slide += arcSpeed)
                {
                    float angle = MathHelper.Lerp(startAngle, endAngle, slide / arcLength);
                    breaths.Add(Scheduler.Instance.RunAndWait(RoutineBreath(user, angle, radius, tiles)));
                    yield return user.WaitSome(5);
                }
                yield return new WaitAll(breaths);
                PopupManager.FinishCollect();
                AfterBreath(user, tiles);
            }
        }

        public abstract IEnumerable<Wait> RoutineBreath(Creature user, float angle, float radius, ICollection<Tile> tiles);

        public virtual void AfterBreath(Creature user, IEnumerable<Tile> tiles)
        {

        }
    }

    class SkillFireBreath : SkillBreathBase
    {
        public SkillFireBreath() : base("Fire Breath", "Description", 1, 1, float.PositiveInfinity)
        {
        }

        public override IEnumerable<Wait> RoutineBreath(Creature user, float angle, float radius, ICollection<Tile> tiles)
        {
            Tile tile = GetImpactTile(user, angle, radius);
            Vector2 direction = Util.AngleToVector(angle);
            Vector2 offset = direction * radius;
            new FireExplosion(user.World, user.VisualTarget, offset * 16f / 20f, angle, 20);
            if (tile != null)
                yield return Scheduler.Instance.RunAndWait(RoutineQuake(user, tile, 1, tiles));
        }

        private IEnumerable<Wait> RoutineQuake(Creature user, Tile impactTile, int radius, ICollection<Tile> tiles)
        {
            var tileSet = impactTile.GetNearby(radius).Where(tile => GetSquareDistance(impactTile, tile) <= radius * radius).Shuffle();
            int chargeTime = Random.Next(10) + 30;
            List<Tile> damageTiles = new List<Tile>();
            foreach (Tile tile in tileSet)
            {
                tile.VisualUnderColor = ChargeColor(user, chargeTime);
                if (!tiles.Contains(tile))
                    damageTiles.Add(tile);
                tiles.Add(tile);
            }
            new FireField(user.World, tileSet, chargeTime);
            new ScreenShakeRandom(user.World, 2, chargeTime + 30, LerpHelper.Invert(LerpHelper.Linear));
            yield return user.WaitSome(chargeTime);
            new ScreenShakeRandom(user.World, 4, 60, LerpHelper.Linear);
            foreach (Tile tile in tileSet)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.3)
                    new FireExplosion(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                else if (Random.NextDouble() < 0.7)
                    new FlameBig(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                tile.VisualUnderColor = () => Color.TransparentBlack;
            }
            foreach (Tile tile in damageTiles)
            {
                foreach (Creature target in tile.Creatures)
                {
                    user.Attack(target, 0, 0, ExplosionAttack);
                }
            }
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Fire, 1.0);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }

        private Func<Color> ChargeColor(Creature user, int time)
        {
            Color black = Color.TransparentBlack;
            Color red = new Color(117, 46, 11);
            Color orange = new Color(241, 153, 20);
            Color yellow = new Color(254, 241, 169);
            int startTime = user.Frame;
            return () =>
            {
                float slide = (float)(user.Frame - startTime) / time;
                if (slide < 0.25f)
                    return Color.Lerp(black, red, slide / 0.25f);
                else if (slide < 0.25f * 2)
                    return Color.Lerp(red, orange, (slide - 0.25f) / 0.25f);
                else if (slide < 0.25f * 3)
                    return Color.Lerp(orange, yellow, (slide - 0.25f * 2) / 0.25f);
                else
                {
                    Color glow = Color.Lerp(Color.Black, Color.White, 0.5f + (float)Math.Sin((user.Frame + time) * 0.4) * 0.5f);
                    return Color.Lerp(yellow, glow, (slide - 0.25f * 3) / 0.25f);
                }
            };
        }
    }
}
