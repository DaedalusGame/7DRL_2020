using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    interface IDrawable
    {
        double DrawOrder
        {
            get;
        }

        IEnumerable<DrawPass> GetDrawPasses();

        void Draw(SceneGame scene, DrawPass pass);
    }
}
