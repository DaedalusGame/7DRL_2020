using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    enum SkillType
    {
        Skill,
        Spell,
    }

    abstract class Skill
    {
        public const int SkillInfoTime = 200;
        public static Random Random = new Random();

        public string Name;
        public string Description;

        public SkillType SkillType = SkillType.Skill; //TODO: Make it a tag system

        public int Priority;
        protected Slider Warmup;
        protected Slider Cooldown;
        protected Slider Uses;
        protected Slider InstantUses;
        public bool IsReady => Warmup.Done && Cooldown.Done && !Uses.Done;

        public virtual bool Hidden(Creature user) => false;
        public virtual bool WaitUse => true;  

        public Skill(string name, string description, int warmup, int cooldown, float uses)
        {
            Name = name;
            Description = description;
            Warmup = new Slider(warmup);
            Cooldown = new Slider(cooldown,cooldown);
            Uses = new Slider(uses);
            InstantUses = new Slider(1);
        }

        public virtual bool CanUse(Creature user)
        {
            return Warmup.Done && Cooldown.Done && !Uses.Done;
        }

        public virtual bool CanEnemyUse(Enemy user)
        {
            return CanUse(user);
        }

        protected void Consume()
        {
            InstantUses += 1;
            if (InstantUses.Done)
            {
                Cooldown.Time = 0;
                InstantUses.Time = 0;
                Uses += 1;
            }
        }

        public string GetTooltip()
        {
            var WarmupLeft = Math.Max(0, Warmup.EndTime - Warmup.Time);
            var CooldownLeft = Math.Max(0, Cooldown.EndTime - Cooldown.Time);
            string name;

            if (IsReady)
                name = $"{Name}";
            else
                name = $"{Game.FormatColor(Color.Gray)}{Name}{Game.FormatColor(Color.White)}";

            if (WarmupLeft > 0)
                return $"- {name} {Game.FormatColor(Color.White)}{Game.FORMAT_BOLD}{WarmupLeft}{Game.FormatSymbol(Symbol.Warmup)}{Game.FORMAT_BOLD}{Game.FormatColor(Color.White)}";
            else if(CooldownLeft > 0)
                return $"- {name} {Game.FormatColor(Color.White)}{Game.FORMAT_BOLD}{CooldownLeft}{Game.FormatSymbol(Symbol.Cooldown)}{Game.FORMAT_BOLD}{Game.FormatColor(Color.White)}";
            else
                return $"- {name}";
        }

        public virtual void Update(Creature user)
        {
            Warmup += 1;
            Cooldown += 1;
        }

        public void ShowSkill(Creature user)
        {
            //new CurrentSkill(user.World, this, time);
            user.VisualColor = user.Flick(user.Flash(user.Static(Color.Black), user.Static(ColorMatrix.Greyscale() * ColorMatrix.Scale(2)), 2, 2), user.Static(Color.White), 30);
            user.World.CurrentSkill = this;
        }

        public void HideSkill(Creature user)
        {
            user.World.CurrentSkill = null;
        }

        protected bool InMeleeRange(Creature user, Func<Creature, bool> filter)
        {
            Point offset = user.Facing.ToOffset();
            foreach (var frontier in user.Mask.GetFrontier(offset.X, offset.Y).Select(p => user.Tile.GetNeighbor(p.X, p.Y)))
            {
                if (frontier.Creatures.Any(filter))
                    return true;
            }
            return false;
        }

        protected bool InCircle(Creature user, Creature target, int radius)
        {
            if (target == null || target.Tile == null)
                return false;
            double pixelRadius = radius * 16 + 8;
            var distance = (user.ActualTarget - target.ActualTarget).LengthSquared();
            return distance <= pixelRadius * pixelRadius;
        }

        protected bool InFrontier(Creature user, Func<Creature, bool> filter)
        {
            foreach (var frontier in user.Mask.GetFullFrontier().Select(p => user.Tile.GetNeighbor(p.X, p.Y)))
            {
                if (frontier.Creatures.Any(filter))
                    return true;
            }
            return false;
        }

        protected bool InLineOfSight(Creature user, Creature target, int distance, int width)
        {
            return InLineOfSight(user, target, user.Facing, distance, width);
        }

        protected bool InLineOfSight(Creature user, Creature target, Facing facing, int distance, int width)
        {
            if (target == null || target.Tile == null)
                return false;

            Rectangle userRect = user.Mask.GetRectangle(user.X, user.Y);
            Rectangle targetRect = target.Mask.GetRectangle(target.X, target.Y);

            int dx = Util.GetDeltaX(userRect, targetRect);
            int dy = Util.GetDeltaY(userRect, targetRect);

            if (!InRange(user, target, distance))
                return false;

            if(Math.Abs(dx) <= width)
            {
                if (dy > 0)
                    return facing == Facing.South;
                if (dy < 0)
                    return facing == Facing.North;
            }
            if(Math.Abs(dy) <= width)
            {
                if (dx > 0)
                    return facing == Facing.East;
                if (dx < 0)
                    return facing == Facing.West;
            }

            return false;
        }
        
        protected bool InRange(Creature user, Creature target, int distance)
        {
            if (target == null || target.Tile == null)
                return false;

            Rectangle userRect = user.Mask.GetRectangle(user.X, user.Y);
            Rectangle targetRect = target.Mask.GetRectangle(target.X, target.Y);

            int dx = Util.GetDeltaX(userRect, targetRect);
            int dy = Util.GetDeltaY(userRect, targetRect);

            return Math.Abs(dx) <= distance && Math.Abs(dy) <= distance;
        }

        protected int GetSquareDistance(Tile a, Tile b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;

            return dx * dx + dy * dy;
        }

        protected Wait GetCurrentAction(Creature creature)
        {
            return creature.CurrentActions;
        }

        public abstract object GetEnemyTarget(Enemy user);

        //public abstract bool IsTargetValid(Creature user, object target);

        public abstract IEnumerable<Wait> RoutineUse(Creature user, object target);
    }
}
