using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class Skill
    {
        public const int SkillInfoTime = 200;
        public static Random Random = new Random();

        public string Name;
        public string Description;

        Slider Warmup;
        Slider Cooldown;
        Slider Uses;

        public virtual bool WaitUse => true;

        public Skill(string name, string description, int warmup, int cooldown, float uses)
        {
            Name = name;
            Description = description;
            Warmup = new Slider(warmup);
            Cooldown = new Slider(cooldown,cooldown);
            Uses = new Slider(uses);
        }

        public virtual bool CanUse(Creature user)
        {
            return Warmup.Done && Cooldown.Done && !Uses.Done;
        }

        protected void Consume()
        {
            Cooldown.Time = 0;
            Uses += 1;
        }

        public virtual void Update()
        {
            Warmup += 1;
            Cooldown += 1;
        }

        protected void ShowSkill(Creature user, int time)
        {
            new CurrentSkill(user.World, this, time);
            user.VisualColor = user.Flick(user.Flash(user.Static(Color.Black), user.Static(ColorMatrix.Greyscale() * ColorMatrix.Scale(2)), 2, 2), user.Static(Color.White), 30);
        }

        protected bool InMeleeRange(Creature user)
        {
            Point offset = user.Facing.ToOffset();
            foreach (var frontier in user.Mask.GetFrontier(offset.X, offset.Y).Select(p => user.Tile.GetNeighbor(p.X, p.Y)))
            {
                if (frontier.Creatures.Any(x => !(x is Enemy)))
                    return true;
            }
            return false;
        }

        protected bool InLineOfSight(Creature user, Creature target, int distance)
        {
            return InLineOfSight(user, target, user.Facing, distance);
        }

        protected bool InLineOfSight(Creature user, Creature target, Facing facing, int distance)
        {
            Rectangle userRect = user.Mask.GetRectangle(user.X, user.Y);
            Rectangle targetRect = target.Mask.GetRectangle(target.X, target.Y);

            int dx = Util.GetDeltaX(userRect, targetRect);
            int dy = Util.GetDeltaY(userRect, targetRect);

            if (!InRange(user, target, distance))
                return false;

            if(dx == 0)
            {
                if (dy > 0)
                    return facing == Facing.South;
                if (dy < 0)
                    return facing == Facing.North;
            }
            else if(dy == 0)
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

        public abstract IEnumerable<Wait> RoutineUse(Creature user);
    }
}
