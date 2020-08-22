using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectDamage : Effect
    {
        public IEffectHolder Holder;
        public double Amount;
        public Element Element;

        public EffectDamage(IEffectHolder holder, double amount, Element element)
        {
            Holder = holder;
            Amount = amount;
            Element = element;
        }

        private void Modify(double damage)
        {
            Amount += damage;
            if (Amount <= 0)
                Remove();
        }

        public override void Apply()
        {
            var damage = EffectManager.GetEffects<EffectDamage>(Holder).Where(x => x.Element == Element && x.Holder == Holder).FirstOrDefault();
            if (damage != null)
                damage.Modify(Amount);
            else if(Amount > 0)
                EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            //EffectManager.RemoveEffect(Holder, this);
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Amount} {Element} Damage ({Holder})";
        }
    }
}
