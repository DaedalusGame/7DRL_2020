using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    interface ITurnTaker
    {
        double TurnSpeed
        {
            get;
        }
        double TurnBuildup
        {
            set;
            get;
        }
        bool TurnReady
        {
            get;
        }

        bool TakeTurn(ActionQueue queue);
    }

    class ActionQueue
    {
        public List<ITurnTaker> TurnTakers = new List<ITurnTaker>();
        public ITurnTaker CurrentTurnTaker;

        public void Cleanup()
        {
            if (CurrentTurnTaker != null && !CurrentTurnTaker.TurnReady)
                CurrentTurnTaker = null;
        }

        public void Step()
        {
            Cleanup();
            if (TurnTakers.Any(x => x.TurnSpeed > 0))
            {
                while (CurrentTurnTaker == null)
                {
                    TurnTakers.ForEach(x => x.IncrementTurn());
                    var fastest = TurnTakers.Aggregate((a, b) => a.TurnBuildup > b.TurnBuildup ? a : b);

                    if (fastest.TurnReady)
                    {
                        CurrentTurnTaker = fastest;
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
            public ITurnTaker Base;
            public double TurnSpeed;
            public double TurnBuildup;

            public bool TurnReady
            {
                get
                {
                    return TurnBuildup >= 1;
                }
            }

            public TheoreticalCharacter(ITurnTaker character)
            {
                Base = character;
                TurnSpeed = character.TurnSpeed;
                TurnBuildup = character.TurnBuildup;
            }

            public void IncrementTurn()
            {
                TurnBuildup += TurnSpeed;
            }

            public void ResetTurn()
            {
                TurnBuildup %= 1;
            }
        }

        public IEnumerable<ITurnTaker> Predict()
        {
            var characters = TurnTakers.Select(x => new TheoreticalCharacter(x)).ToList();
            if (characters.Any(x => x.TurnSpeed > 0))
            {
                while (true)
                {
                    characters.ForEach(x => x.IncrementTurn());
                    var fastest = characters.Aggregate((a, b) => a.TurnBuildup > b.TurnBuildup ? a : b);
                    if (fastest.TurnReady)
                    {
                        yield return fastest.Base;
                        fastest.ResetTurn();
                    }
                }
            }
        }
    }
}
