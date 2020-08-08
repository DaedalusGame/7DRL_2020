using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class SkillEnderClaw : SkillAttackBase
    {
        public SkillEnderClaw() : base("Attack", "Ender Claw", 0, 1, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Slash, 0.5);
            attack.Elements.Add(Element.TheEnd, 1.0);
            return attack;
        }
    }

    class SkillEnderRam : SkillRamBase
    {
        public SkillEnderRam() : base("Ender Ram", "Physical The End attack. Destroys terrain.", 1, 5, float.PositiveInfinity)
        {
            MaxDistance = 9;
            MaxTotalHits = 6;
            MaxWallHits = 4;
            MaxCreatureHits = 999999;
            DestroyWalls = true;
        }

        protected override Attack RamAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.Elements.Add(Element.TheEnd, 0.5);
            return attack;
        }
    }

    class SkillEnderMow : SkillRamBase
    {
        public SkillEnderMow() : base("Ender Mow", "Physical The End attack", 1, 3, float.PositiveInfinity)
        {
            MaxDistance = 3;
            MaxTotalHits = 2;
            MaxWallHits = 0;
            MaxCreatureHits = 999999;
            DestroyWalls = false;
            CheckTarget = false;
        }

        protected override Attack RamAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.Elements.Add(Element.TheEnd, 0.5);
            return attack;
        }
    }

    class SkillEnderPowerUp : Skill
    {
        public override bool Hidden(Creature user) => true;

        public SkillEnderPowerUp() : base("Power Up", "Enrage.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user.HasStatusEffect<PoweredUp>())
                return false;
            return base.CanUse(user);
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            SpriteReference cinder = SpriteLoader.Instance.AddSprite("content/cinder_ender");
            new FlarePower(user.World, cinder, user, 50);
            new ScreenFlashPowerUp(user, () => ColorMatrix.Ender(), 100, 100, 50, 50);
            user.AddStatusEffect(new PoweredUp());
            yield return user.WaitSome(20);
        }
    }

    class SkillEnderFlare : Skill
    {
        public override bool Hidden(Creature user) => !user.HasStatusEffect<PoweredUp>();

        public SkillEnderFlare() : base("Ender Flare", "Ranged The End Attack.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && user.HasStatusEffect<PoweredUp>();
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 4);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                Consume();
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var effect = new FlareCharge(user.World, SpriteLoader.Instance.AddSprite("content/cinder_ender"), user, () => targetCreature.VisualTarget, 200);

                yield return user.WaitSome(50);
                new ScreenShakeRandom(user.World, 2, 150, LerpHelper.Invert(LerpHelper.Linear));
                yield return new WaitEffect(effect);
                new ScreenFlashLocal(user.World, () => ColorMatrix.Ender(), targetCreature.VisualTarget, 60, 150, 80, 50);
                new EnderNuke(user.World, SpriteLoader.Instance.AddSprite("content/nuke_ender"), targetCreature.VisualTarget, 0.6f, 80);
                new ScreenShakeRandom(user.World, 8, 80, LerpHelper.QuarticIn);
                //new BigExplosion(user.World, () => target.VisualTarget, (pos, time) => new EnderExplosion(user.World, pos, Vector2.Zero, time));
                yield return user.WaitSome(10);
                user.Attack(targetCreature, 0, 0, FlareAttack);
                yield return targetCreature.CurrentAction;
                yield return user.WaitSome(20);
            }
        }

        private static Attack FlareAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.TheEnd, 1.0);
            return attack;
        }
    }

    class SkillEnderBlast : Skill
    {
        public override bool Hidden(Creature user) => true;

        public SkillEnderBlast() : base("Ender Blast", "Frees Ender Erebizo from rock.", 0, 0, float.PositiveInfinity)
        {
            Priority = 10;
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && !user.Tiles.Any(tile => !tile.Opaque);
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            yield return user.WaitSome(50);
            var tileSet = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 1).Shuffle(Random);
            List<Wait> quakes = new List<Wait>();
            HashSet<Tile> tiles = new HashSet<Tile>();
            PopupManager.StartCollect();
            foreach (Tile tile in tileSet.Take(5))
            {
                quakes.Add(Scheduler.Instance.RunAndWait(RoutineQuake(user, tile, 2, tiles)));
            }
            new ScreenFlashLocal(user.World, () => ColorMatrix.Ender(), user.VisualTarget, 60, 150, 100, 50);
            yield return new WaitAll(quakes);
            PopupManager.FinishCollect();
            yield return user.WaitSome(20);
        }

        private IEnumerable<Wait> RoutineQuake(Creature user, Tile impactTile, int radius, ICollection<Tile> tiles)
        {
            var tileSet = impactTile.GetNearby(radius).Where(tile => tile.Opaque).Where(tile => GetSquareDistance(impactTile, tile) <= radius * radius).Shuffle(Random);
            int chargeTime = 60;
            List<Tile> damageTiles = new List<Tile>();
            foreach (Tile tile in tileSet)
            {
                tile.VisualUnderColor = ChargeColor(user, chargeTime);
                if (!tiles.Contains(tile))
                    damageTiles.Add(tile);
                tiles.Add(tile);

            }
            new ScreenShakeRandom(user.World, 4, chargeTime + 60, LerpHelper.Invert(LerpHelper.Linear));
            yield return user.WaitSome(chargeTime);
            new LightningField(user.World, SpriteLoader.Instance.AddSprite("content/lightning_ender"), tileSet, 60);
            yield return user.WaitSome(60);
            new ScreenShakeRandom(user.World, 8, 60, LerpHelper.Linear);
            foreach (Tile tile in tileSet)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.7)
                    new EnderExplosion(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                tile.VisualUnderColor = () => Color.TransparentBlack;
                tile.MakeFloor();
            }
        }

        private Func<Color> ChargeColor(Creature user, int time)
        {
            Color black = Color.TransparentBlack;
            Color darkPurple = new Color(103, 21, 138);
            Color purple = new Color(174, 56, 224);
            Color blue = new Color(196, 223, 251);
            int startTime = user.Frame;
            return () =>
            {
                float slide = (float)(user.Frame - startTime) / time;
                if (slide < 0.25f)
                    return Color.Lerp(black, darkPurple, slide / 0.25f);
                else if (slide < 0.25f * 2)
                    return Color.Lerp(darkPurple, purple, (slide - 0.25f) / 0.25f);
                else if (slide < 0.25f * 3)
                    return Color.Lerp(purple, blue, (slide - 0.25f * 2) / 0.25f);
                else
                {
                    Color glow = Color.Lerp(Color.Black, Color.White, 0.5f + (float)Math.Sin((user.Frame + time) * 0.4) * 0.5f);
                    return Color.Lerp(blue, glow, (slide - 0.25f * 3) / 0.25f);
                }
            };
        }
    }

    class SkillEnderQuake : Skill
    {
        public override bool Hidden(Creature user) => !user.HasStatusEffect<PoweredUp>();

        public SkillEnderQuake() : base("Ender Quake", "Ranged The End Attack.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && user.HasStatusEffect<PoweredUp>();
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            ShowSkill(user);
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            //TODO: roar?
            //TODO: jump visual, screenshake, screen distort
            yield return user.WaitSome(50);
            user.VisualPosition = user.SlideJump(user.VisualPosition(), new Vector2(user.X, user.Y) * 16, 16, LerpHelper.Linear, 20);
            yield return user.WaitSome(20);
            new ScreenShakeRandom(user.World, 6, 30, LerpHelper.Linear);
            new SeismArea(user.World, user.Tiles, 10);
            var tileSet = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 6).Shuffle(Random);
            List<Wait> quakes = new List<Wait>();
            HashSet<Tile> tiles = new HashSet<Tile>();
            PopupManager.StartCollect();
            foreach (Tile tile in tileSet.Take(8))
            {
                quakes.Add(Scheduler.Instance.RunAndWait(RoutineQuake(user, tile, 3, tiles)));
            }
            new ScreenFlashLocal(user.World, () => ColorMatrix.Ender(), user.VisualTarget, 60, 150, 100, 50);
            yield return new WaitAll(quakes);
            PopupManager.FinishCollect();
            yield return user.WaitSome(20);
        }

        private IEnumerable<Wait> RoutineQuake(Creature user, Tile impactTile, int radius, ICollection<Tile> tiles)
        {
            var tileSet = impactTile.GetNearby(radius).Where(tile => GetSquareDistance(impactTile, tile) <= radius * radius).Shuffle(Random);
            int chargeTime = Random.Next(10) + 60;
            List<Tile> damageTiles = new List<Tile>();
            foreach (Tile tile in tileSet)
            {
                tile.VisualUnderColor = ChargeColor(user, chargeTime);
                if (!tiles.Contains(tile))
                    damageTiles.Add(tile);
                tiles.Add(tile);
            }
            new ScreenShakeRandom(user.World, 4, chargeTime + 60, LerpHelper.Invert(LerpHelper.Linear));
            yield return user.WaitSome(chargeTime);
            new LightningField(user.World, SpriteLoader.Instance.AddSprite("content/lightning_ender"), tileSet, 60);
            yield return user.WaitSome(60);
            new ScreenShakeRandom(user.World, 8, 60, LerpHelper.Linear);
            foreach (Tile tile in tileSet)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.7)
                    new EnderExplosion(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                tile.VisualUnderColor = () => Color.TransparentBlack;
            }
            foreach (Tile tile in damageTiles)
            {
                foreach (Creature creature in tile.Creatures)
                {
                    user.Attack(creature, 0, 0, AttackQuake);
                }
            }
        }

        private Attack AttackQuake(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.TheEnd, 1.0);
            return attack;
        }

        private Func<Color> ChargeColor(Creature user, int time)
        {
            Color black = Color.TransparentBlack;
            Color darkPurple = new Color(103, 21, 138);
            Color purple = new Color(174, 56, 224);
            Color blue = new Color(196, 223, 251);
            int startTime = user.Frame;
            return () =>
            {
                float slide = (float)(user.Frame - startTime) / time;
                if (slide < 0.25f)
                    return Color.Lerp(black, darkPurple, slide / 0.25f);
                else if (slide < 0.25f * 2)
                    return Color.Lerp(darkPurple, purple, (slide - 0.25f) / 0.25f);
                else if (slide < 0.25f * 3)
                    return Color.Lerp(purple, blue, (slide - 0.25f * 2) / 0.25f);
                else
                {
                    Color glow = Color.Lerp(Color.Black, Color.White, 0.5f + (float)Math.Sin((user.Frame + time) * 0.4) * 0.5f);
                    return Color.Lerp(blue, glow, (slide - 0.25f * 3) / 0.25f);
                }
            };
        }
    }
}
