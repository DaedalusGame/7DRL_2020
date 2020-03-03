using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class VisualEffect : IGameObject
    {
        public SceneGame World { get; set; }
        public double DrawOrder => 0;
        bool IGameObject.Remove { get; set; }

        public Slider Frame;

        public VisualEffect(SceneGame world)
        {
            World = world;
            World.GameObjects.Add(this);
        }

        public void OnDestroy()
        {
            //NOOP
        }

        public virtual void Update()
        {
            Frame += 1;
        }

        public abstract IEnumerable<DrawPass> GetDrawPasses();

        public abstract void Draw(SceneGame scene, DrawPass pass);
    }

    abstract class Particle : VisualEffect
    {
        public Vector2 Position
        {
            get;
            set;
        }

        public Particle(SceneGame world, Vector2 position) : base(world)
        {
            Position = position;
        }
    }

    class DamagePopup : Particle
    {
        public string Text;
        public TextParameters Parameters;
        public Vector2 Offset => new Vector2(0, -16) * (float)LerpHelper.QuadraticOut(0, 1, Frame.Slide);

        public DamagePopup(SceneGame world, Vector2 position, string text, TextParameters parameters, int time) : base(world, position)
        {
            Text = text;
            Parameters = parameters;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.UI;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            string fit = FontUtil.FitString(Text, Parameters);
            var height = FontUtil.GetStringHeight(fit);
            Vector2 pos = Vector2.Transform(Position + Offset, scene.WorldTransform);
            scene.DrawText(Text, pos - new Vector2(0, height / 2), Alignment.Center, Parameters);
        }
    }
}
