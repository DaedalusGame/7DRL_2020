using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnTrail : EffectEvent<TrailEvent>
    {
        public OnTrail(IEffectHolder holder, Func<TrailEvent, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }

    class TrailEvent
    {
        public Skills.Projectile Projectile;
        public Tile Tile;

        public TrailEvent(Skills.Projectile projectile, Tile tile)
        {
            Projectile = projectile;
            Tile = tile;
        }
    }
}
