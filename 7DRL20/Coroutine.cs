using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Result<T>
    {
        public bool HasResult;
        public T Value;

        public void Write(T value)
        {
            Value = value;
            HasResult = true;
        }
    }

    public abstract class Wait
    {
        public abstract bool Done { get; }

        public abstract void Update();

        public static Wait NoWait = new WaitDone();
    }

    public class WaitDone : Wait
    {
        public override bool Done => true;

        public override void Update()
        {
            //NOOP
        }
    }

    class WaitAllTurns : Wait
    {
        TurnTaker TurnTaker;

        public WaitAllTurns(TurnTaker turnTaker)
        {
            TurnTaker = turnTaker;
        }

        public override bool Done => !TurnTaker.HasImmediateTurns();

        public override void Update()
        {
            //NOOP
        }
    }

    class WaitMenu : Wait
    {
        Menu Menu;

        public override bool Done => Menu.ShouldClose;

        public WaitMenu(Menu menu)
        {
            Menu = menu;
        }

        public override void Update()
        {
            //NOOP
        }
    }

    public class WaitAll : Wait
    {
        IEnumerable<Wait> Waits;

        public override bool Done => Waits.All(wait => wait.Done);

        public WaitAll(IEnumerable<Wait> waits)
        {
            Waits = waits;
        }

        public override void Update()
        {
            //NOOP
        }
    }

    public class WaitWorld : Wait
    {
        List<Wait> Waits = new List<Wait>();

        public override bool Done => Waits.All(wait => wait.Done);

        public WaitWorld()
        {
        }

        public void Add(Wait wait)
        {
            Waits.Add(wait);
        }

        public override void Update()
        {
            foreach (var wait in Waits)
                wait.Update();
            Waits.RemoveAll(wait => wait.Done);
        }
    }

    class WaitGameObject : WaitWorld
    {
        IGameObject GameObject;

        public override bool Done => GameObject.Destroyed || base.Done;

        public WaitGameObject(IGameObject gameObject)
        {
            GameObject = gameObject;
        }

        
    }

    public class WaitForInput : Wait
    {
        public override bool Done => false;

        public WaitForInput()
        {
        }

        public override void Update()
        {
            //NOOP
        }
    }

    public class WaitTime : Wait
    {
        int Frames;

        public override bool Done => Frames <= 0;

        public WaitTime(int frames)
        {
            Frames = frames;
        }

        public override void Update()
        {
            Frames--;
        }
    }

    public class WaitCoroutine : Wait
    {
        Coroutine Coroutine;

        public override bool Done => Coroutine.Done;

        public WaitCoroutine(Coroutine coroutine)
        {
            Coroutine = coroutine;
        }

        public override void Update()
        {
            //NOOP
        }
    }

    class WaitEffect : Wait
    {
        VisualEffect Effect;

        public override bool Done => Effect.Destroyed;

        public WaitEffect(VisualEffect effect)
        {
            Effect = effect;
        }

        public override void Update()
        {
            //NOOP
        }
    }

    class WaitBullet : Wait
    {
        Projectile Bullet;

        public override bool Done => Bullet.Hit;

        public WaitBullet(Projectile bullet)
        {
            Bullet = bullet;
        }

        public override void Update()
        {
            //NOOP
        }
    }

    public class Coroutine
    {
        IEnumerator<Wait> Enumerator;
        public bool Done;
        public Wait CurrentWait => Enumerator.Current;

        public Coroutine(IEnumerable<Wait> routine)
        {
            Enumerator = routine.GetEnumerator();
            Done = !Enumerator.MoveNext();
        }

        public void Update()
        {
            Wait wait = Enumerator.Current;
            if (wait == null || wait.Done)
            {
                Done = !Enumerator.MoveNext();
            }
            else if (wait != null)
            {
                wait.Update();
            }
        }
    }
}
