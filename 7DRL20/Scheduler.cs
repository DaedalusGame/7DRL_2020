using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Scheduler
    {
        public static Scheduler Instance
        {
            get;
            private set;
        }

        public static void Init()
        {
            Instance = new Scheduler();
        }

        public List<Coroutine> Coroutines = new List<Coroutine>();
        public Queue<Coroutine> CoroutineQueue = new Queue<Coroutine>();

        public void Run(Coroutine routine)
        {
            CoroutineQueue.Enqueue(routine);
        }

        public void Run(IEnumerable<Wait> routine)
        {
            Run(new Coroutine(routine));
        }

        public Wait RunAndWait(Coroutine routine)
        {
            var wait = new WaitCoroutine(routine);
            Run(routine);
            return wait;
        }

        public Wait RunAndWait(IEnumerable<Wait> routine)
        {
            return RunAndWait(new Coroutine(routine));
        }

        public void RunTimer(System.Action action, Wait wait)
        {
            Run(new Coroutine(RunTimerInternal(action, wait)));
        }

        private IEnumerable<Wait> RunTimerInternal(System.Action action, Wait wait)
        {
            yield return wait;
            action();
        }

        public void Update()
        {
            while (CoroutineQueue.Count > 0)
            {
                var routine = CoroutineQueue.Dequeue();
                if (!routine.Done)
                    Coroutines.Add(routine);
            }
            foreach (var routine in Coroutines)
            {
                routine.Update();
            }
            Coroutines.RemoveAll(routine => routine.Done);
        }
    }
}
