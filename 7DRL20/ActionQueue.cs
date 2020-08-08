using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class TurnTaker
    {
        public delegate Wait TurnDelegate(Turn turn);

        ActionQueue Queue;

        public abstract object Owner { get; }
        public abstract double Speed { get; }
        public abstract bool RemoveFromQueue { get; }
        public double Buildup;
        public bool Ready => Buildup >= 1;
        public virtual bool Controllable(Creature player) => false;

        public int ExtraTurns;

        public TurnTaker(ActionQueue queue)
        {
            Queue = queue;
        }

        public virtual Wait StartTurn(Turn turn)
        {
            return Wait.NoWait;
        }

        public virtual Wait TakeTurn(Turn turn)
        {
            return Wait.NoWait;
        }

        public virtual Wait EndTurn(Turn turn)
        {
            return Wait.NoWait;
        }

        public void AddImmediate(Turn parent)
        {
            Queue.AddImmediate(new Turn(parent, this));
        }

        public void AddImmediate()
        {
            Queue.AddImmediate(new Turn(Queue, this));
        }

        public void AddExtraTurns(int turns)
        {
            ExtraTurns += turns;
        }

        public void IncrementTurn()
        {
            Buildup += Speed;
        }

        public void ResetTurn()
        {
            Buildup -= 1;
        }

        public override string ToString()
        {
            return $"{Owner}: Buildup {Buildup}";
        }
    }

    class TurnTakerWorld : TurnTaker
    {
        SceneGame World;

        public TurnTakerWorld(ActionQueue queue, SceneGame world) : base(queue)
        {
            World = world;
        }

        public override object Owner => World;
        public override double Speed => 0;
        public override bool RemoveFromQueue => false;

        public override Wait StartTurn(Turn turn)
        {
            return World.StartTurn(turn);
        }

        public override Wait TakeTurn(Turn turn)
        {
            return World.TakeTurn(turn);
        }

        public override Wait EndTurn(Turn turn)
        {
            return World.EndTurn(turn);
        }
    }

    class TurnTakerCreatureControl : TurnTaker
    {
        Creature Creature;

        public TurnTakerCreatureControl(ActionQueue queue, Creature creature) : base(queue)
        {
            Creature = creature;
        }

        public override bool Controllable(Creature player) => Creature.IsControllable(player);
        public override object Owner => Creature;
        public override double Speed => Creature.GetStat(Stat.Speed);
        public override bool RemoveFromQueue => Creature.Destroyed;

        public override Wait StartTurn(Turn turn)
        {
            return Creature.StartTurn(turn);
        }

        public override Wait TakeTurn(Turn turn)
        {
            return Creature.TakeTurn(turn);
        }

        public override Wait EndTurn(Turn turn)
        {
            return Creature.EndTurn(turn);
        }
    }

    class TurnTakerCreatureNormal : TurnTaker
    {
        Creature Entity;

        public TurnTakerCreatureNormal(ActionQueue queue, Creature entity) : base(queue)
        {
            Entity = entity;
        }

        public override object Owner => Entity;
        public override double Speed => 1;
        public override bool RemoveFromQueue => Entity.Destroyed;

        public override Wait TakeTurn(Turn turn)
        {
            return Entity.NormalTurn(turn);
        }
    }

    class TurnTakerCloud : TurnTaker
    {
        Cloud Cloud;

        public TurnTakerCloud(ActionQueue queue, Cloud cloud) : base(queue)
        {
            Cloud = cloud;
        }

        public override object Owner => Cloud;
        public override double Speed => 1;
        public override bool RemoveFromQueue => Cloud.Destroyed;

        public override Wait TakeTurn(Turn turn)
        {
            return Cloud.NormalTurn(turn);
        }
    }

    enum TurnPhase
    {
        Start,
        Tick,
        End,
        Delete,
    }

    class Turn
    {
        public ActionQueue Queue;
        public TurnTaker TurnTaker;
        public Turn Root;
        public Wait Wait;
        public TurnPhase Phase = TurnPhase.Start;

        public Turn(ActionQueue queue, TurnTaker turnTaker)
        {
            Queue = queue;
            TurnTaker = turnTaker;
        }

        public Turn(Turn root, TurnTaker turnTaker)
        {
            Queue = root.Queue;
            TurnTaker = turnTaker;
            Root = root;
        }

        public Wait StartTurn()
        {
            Wait wait = TurnTaker.StartTurn(this);
            Phase = TurnPhase.Tick;
            return wait;
        }

        public bool TakeTurn()
        {
            Wait = TurnTaker.TakeTurn(this);
            if (Wait != null)
            {
                Phase = TurnPhase.End;
                return true;
            }
            return false;
        }

        public Wait EndTurn()
        {
            Wait wait = TurnTaker.EndTurn(this);
            for (int i = 0; i < TurnTaker.ExtraTurns; i++)
            {
                Queue.AddImmediate(new Turn(this, TurnTaker));
            }
            TurnTaker.ExtraTurns = 0;
            Phase = TurnPhase.Delete;
            return wait;
        }

        public void End()
        {
            Phase = TurnPhase.End;
        }
    }

    class ActionQueue
    {
        public SceneGame World;

        public List<TurnTaker> TurnTakers = new List<TurnTaker>();
        public List<Turn> ImmediateTurns = new List<Turn>();
        public Turn CurrentTurn;
        public TurnTaker WorldTurnTaker;

        public List<Turn> PredictedTurns = new List<Turn>();

        public ActionQueue(SceneGame world)
        {
            World = world;
            WorldTurnTaker = new TurnTakerWorld(this, world);
        }

        public void Add(TurnTaker turnTaker)
        {
            TurnTakers.Add(turnTaker);
        }

        public void AddImmediate(Turn turn)
        {
            ImmediateTurns.Add(turn);
        }

        public void Cleanup()
        {
            if (CurrentTurn != null && CurrentTurn.Phase == TurnPhase.Delete)
                CurrentTurn = null;
            TurnTakers.RemoveAll(x => x.RemoveFromQueue);
        }

        public void Step()
        {
            Cleanup();

            if (CurrentTurn == null && ImmediateTurns.Any())
            {
                CurrentTurn = ImmediateTurns.First();
                ImmediateTurns.RemoveAt(0);
                return;
            }

            if (TurnTakers.Any(x => x.Speed > 0))
            {
                while (CurrentTurn == null)
                {
                    if (!TurnTakers.Any(x => x.Ready))
                    {
                        TurnTakers.ForEach(x => x.IncrementTurn());
                        CurrentTurn = new Turn(this, WorldTurnTaker);
                        break;
                    }
                    var fastest = TurnTakers.OrderByDescending(x => x.Buildup);

                    foreach (var turnTaker in fastest)
                    {
                        if (turnTaker.Ready)
                        {
                            CurrentTurn = new Turn(this, turnTaker);
                            turnTaker.ResetTurn();
                            break;
                        }
                    }
                }
            }
            else
            {
                //True Victory
            }
        }

        class TheoreticalCharacter
        {
            public TurnTaker Base;
            public double Speed;
            public double Buildup;
            public int ExtraTurns;

            public bool Ready => Buildup >= 1;

            public TheoreticalCharacter(TurnTaker character)
            {
                Base = character;
                Speed = character.Speed;
                Buildup = character.Buildup;
                ExtraTurns = character.ExtraTurns;
            }

            public void IncrementTurn()
            {
                Buildup += Speed;
            }

            public void ResetTurn()
            {
                Buildup %= 1;
            }
        }

        public IEnumerable<Turn> Predict()
        {
            var characters = TurnTakers.Select(x => new TheoreticalCharacter(x)).ToList();
            foreach (Turn turn in ImmediateTurns)
                yield return turn;

            if (characters.Any(x => x.Speed > 0))
            {
                while (true)
                {
                    if (!characters.Any(x => x.Ready))
                        foreach (var character in characters)
                            characters.ForEach(x => x.IncrementTurn());
                    var fastest = characters.OrderByDescending(x => x.Buildup);

                    foreach (var turnTaker in fastest)
                    {
                        if (turnTaker.Ready)
                        {
                            var turn = new Turn(this, turnTaker.Base);
                            yield return turn;
                            turnTaker.ResetTurn();
                            for (int i = 0; i < turnTaker.ExtraTurns; i++)
                                yield return new Turn(turn, turnTaker.Base);
                            break;
                        }
                    }
                }
            }
        }
    }
}
