using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    interface IGameObject
    {
        SceneGame World
        {
            get;
            set;
        }

        double DrawOrder
        {
            get;
        }

        bool Destroyed
        {
            set;
            get;
        }

        void Update();

        void OnDestroy();

        bool ShouldDraw(Map map);

        IEnumerable<DrawPass> GetDrawPasses();

        void Draw(SceneGame scene, DrawPass pass);
    }
}
