using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoguelikeEngine.Skills;

namespace RoguelikeEngine.Effects
{
    class OnShoot : EffectEvent<ShootEvent>
    {
        public OnShoot(IEffectHolder holder, Func<ShootEvent, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }

    class ShootEvent
    {
        public Skills.Projectile Projectile;
        public Creature Creature;
        public Tile Tile;

        public ShootEvent(Skills.Projectile projectile, Creature creature, Tile tile)
        {
            Projectile = projectile;
            Creature = creature;
            Tile = tile;
        }
    }
}
