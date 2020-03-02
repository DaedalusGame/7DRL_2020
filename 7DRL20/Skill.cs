using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class Skill
    {
        public string Name;
        public string Description;

        public virtual bool CanUse => true;

        public Skill(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public virtual void Update()
        {
        }

        public abstract IEnumerable<Wait> RoutineUse(Creature user);
    }
}
