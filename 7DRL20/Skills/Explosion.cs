using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class Explosion
    {
        public Creature Attacker;

        public Vector2 Origin;
        public IEnumerable<Tile> Tiles;

        public AttackDelegate Attack;
        public double MiningPower;

        public IEffectHolder Fault;
        public int ReactionLevel;

        public Explosion(Creature attacker, IEnumerable<Tile> tiles, Vector2 origin)
        {
            Attacker = attacker;
            Origin = origin;
            Tiles = tiles;
        }

        public Wait Run()
        {
            var waitForDamage = new List<Wait>();
            foreach (var explosionTile in Tiles)
            {
                foreach (var targetCreature in explosionTile.Creatures)
                {
                    waitForDamage.Add(Attacker.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - Origin), Attack));
                }
                if (explosionTile is IMineable mineable)
                {
                    MineEvent fracture = new MineEvent(Attacker, null, 100)
                    {
                        Fault = Fault,
                        ReactionLevel = ReactionLevel + 1
                    };
                    waitForDamage.Add(mineable.Mine(fracture));
                }
            }
            return new WaitAll(waitForDamage);
        }
    }
}
