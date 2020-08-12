using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Symbol
    {
        public static List<Symbol> AllSymbols = new List<Symbol>();

        public static Symbol Cooldown = new Symbol(SpriteLoader.Instance.AddSprite("content/stat_cooldown"));
        public static Symbol Warmup = new Symbol(SpriteLoader.Instance.AddSprite("content/stat_warmup"));

        public int ID;
        public SpriteReference Sprite;

        public Symbol(SpriteReference sprite)
        {
            ID = AllSymbols.Count;
            Sprite = sprite;
            AllSymbols.Add(this);
        }

        public virtual void DrawIcon(Scene scene, Vector2 pos)
        {
            scene.DrawSprite(Sprite, 0, pos, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
    }

    class SymbolElement : Symbol
    {
        Element Element;

        public SymbolElement(SpriteReference sprite, Element element) : base(sprite)
        {
            Element = element;
        }

        public override void DrawIcon(Scene scene, Vector2 pos)
        {
            Element.Symbol.DrawIcon(scene, pos);
            base.DrawIcon(scene, pos);
        }
    }
}
