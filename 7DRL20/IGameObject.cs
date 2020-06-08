using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    interface IGameObject : IDrawable
    {
        SceneGame World
        {
            get;
            set;
        }

        bool Destroyed
        {
            set;
            get;
        }

        void Update();

        void OnDestroy();

        bool ShouldDraw(Map map);
    }
}
