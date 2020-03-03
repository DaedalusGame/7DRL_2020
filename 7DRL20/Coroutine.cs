using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
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
