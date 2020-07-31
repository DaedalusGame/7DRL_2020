using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectLastHit : Effect
    {
        public IEffectHolder Holder;
        public Creature Attacker;
        public double TotalDamage;

        public EffectLastHit(IEffectHolder holder, Creature attacker, double damage)
        {
            Holder = holder;
            Attacker = attacker;
            TotalDamage = damage;
        }

        private void Modify(double damage)
        {
            TotalDamage += damage;
            if (TotalDamage <= 0)
                Remove();
        }

        public override void Apply()
        {
            var damage = EffectManager.GetEffects<EffectLastHit>(Holder).Where(x => x.Attacker == Attacker && x.Holder == Holder).FirstOrDefault();
            if (damage != null)
                damage.Modify(TotalDamage);
            else if (TotalDamage > 0)
                EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override string ToString()
        {
            return $"Hit by {Attacker}: {TotalDamage} Total Damage ({Holder})";
        }
    }
}
