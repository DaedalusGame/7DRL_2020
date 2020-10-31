using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.VisualEffects
{
    abstract class ScreenFlash : VisualEffect
    {
        public abstract ColorMatrix Color
        {
            get;
        }
        public bool Delete = true;

        public ScreenFlash(SceneGame world, float time) : base(world)
        {
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done && Delete)
            {
                this.Destroy();
            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }
    }

    class ScreenFlashLocal : ScreenFlash
    {
        public virtual Vector2 Position
        {
            get;
            set;
        }
        Func<ColorMatrix> ColorFunction;

        float FullRadius;
        float FalloffRadius;

        int Time;
        int FalloffTime;

        public override ColorMatrix Color => ColorMatrix.Lerp(ColorMatrix.Identity, ColorFunction(), GetFalloff());

        public ScreenFlashLocal(SceneGame world, Func<ColorMatrix> colorFunction, Vector2 position, float fullRadius, float falloffRadius, int time, int falloffTime) : base(world, time + falloffTime)
        {
            Position = position;
            ColorFunction = colorFunction;
            FullRadius = fullRadius;
            FalloffRadius = falloffRadius;
            Time = time;
            FalloffTime = falloffTime;
        }

        float GetTimeFalloff()
        {
            if (Frame.Time < Time)
                return 1.0f;
            else if (Frame.Time < Time + FalloffTime)
                return (float)LerpHelper.CubicIn(1, 0, (Frame.Time - Time) / FalloffTime);
            else
                return 0.0f;
        }

        float GetFalloff()
        {
            float timeFalloff = GetTimeFalloff();
            Creature player = World.Player;
            if (player == null)
                return 0.0f;
            Vector2 dist = player.VisualTarget - Position;
            float length = dist.Length();
            if (length < FullRadius)
                return 1.0f * timeFalloff;
            else if (length < FullRadius + FalloffRadius)
                return (float)LerpHelper.CubicIn(1, 0, (length - FullRadius) / FalloffRadius) * timeFalloff;
            else
                return 0.0f;
        }
    }

    class ScreenFlashPowerUp : ScreenFlashLocal
    {
        Creature Creature;

        public override Vector2 Position
        {
            get
            {
                return Creature.VisualTarget;
            }
            set
            {
                //NOOP
            }
        }

        public ScreenFlashPowerUp(Creature creature, Func<ColorMatrix> colorFunction, float fullRadius, float falloffRadius, int time, int falloffTime) : base(creature.World, colorFunction, Vector2.Zero, fullRadius, falloffRadius, time, falloffTime)
        {
            Creature = creature;
        }

        public override void Update()
        {
            if (Creature.HasStatusEffect(statusEffect => statusEffect is PoweredUp))
            {
                Frame.Time = 0;
            }
            base.Update();
        }
    }

    class ScreenFade : ScreenFlash
    {
        Func<ColorMatrix> ColorFunction;
        LerpHelper.Delegate Lerp;

        public override ColorMatrix Color => ColorMatrix.Lerp(ColorMatrix.Identity, ColorFunction(), (float)Lerp(0, 1, Frame.Slide));

        public ScreenFade(SceneGame world, Func<ColorMatrix> color, LerpHelper.Delegate lerp, bool delete, float time) : base(world, time)
        {
            ColorFunction = color;
            Lerp = lerp;
            Delete = delete;
        }
    }

    abstract class ScreenGlitch : VisualEffect
    {
        public abstract GlitchParams Glitch
        {
            get;
        }

        public ScreenGlitch(SceneGame world) : base(world)
        {
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }
    }

    class ScreenGlitchFlash : ScreenGlitch
    {
        Func<float, GlitchParams> GlitchFunction;

        public override GlitchParams Glitch => GlitchFunction(Frame.Slide);

        public ScreenGlitchFlash(SceneGame world, Func<float, GlitchParams> glitch, int time) : base(world)
        {
            GlitchFunction = glitch;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {
                this.Destroy();
            }
        }
    }

    class ScreenGlitchFade : ScreenGlitch
    {
        Func<float, GlitchParams> FadeInFunction;
        Func<float, GlitchParams> FadeOutFunction;
        int TimeIn;
        int TimeOut;

        public override GlitchParams Glitch => Frame.Time < TimeIn ? FadeInFunction(Frame.GetSubSlide(0, TimeIn)) : FadeOutFunction(Frame.GetSubSlide(TimeIn, TimeIn + TimeOut));

        public ScreenGlitchFade(SceneGame world, Func<float, GlitchParams> fadeIn, Func<float, GlitchParams> fadeOut, int timeIn, int timeOut) : base(world)
        {
            FadeInFunction = fadeIn;
            FadeOutFunction = fadeOut;
            TimeIn = timeIn;
            TimeOut = timeOut;
            Frame = new Slider(timeIn + timeOut);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {
                this.Destroy();
            }
        }
    }

    abstract class ScreenShake : VisualEffect
    {
        public Vector2 Offset;

        public ScreenShake(SceneGame world, int time) : base(world)
        {
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {
                this.Destroy();
            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }
    }

    class ScreenShakeRandom : ScreenShake
    {
        float Amount;
        LerpHelper.Delegate Lerp;

        public ScreenShakeRandom(SceneGame world, float amount, int time, LerpHelper.Delegate lerp) : base(world, time)
        {
            Lerp = lerp;
            Amount = amount;
        }

        public override void Update()
        {
            base.Update();

            double amount = Lerp(Amount, 0, Frame.Slide);
            double shakeAngle = Random.NextDouble() * Math.PI * 2;
            int x = (int)Math.Round(Math.Cos(shakeAngle) * amount);
            int y = (int)Math.Round(Math.Sin(shakeAngle) * amount);
            Offset = new Vector2(x, y);
        }
    }

    class ScreenShakeJerk : ScreenShake
    {
        Vector2 Jerk;

        public ScreenShakeJerk(SceneGame world, Vector2 jerk, int time) : base(world, time)
        {
            Jerk = jerk;
        }

        public override void Update()
        {
            base.Update();

            float amount = (1 - Frame.Slide);
            Offset = Jerk * amount;
        }
    }
}
