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

        public static Symbol FuelBar = new SymbolBar(SpriteLoader.Instance.AddSprite("content/bar_fuel"));

        public int ID;
        public SpriteReference Sprite;

        public Symbol(SpriteReference sprite)
        {
            ID = AllSymbols.Count;
            Sprite = sprite;
            AllSymbols.Add(this);
        }

        public virtual void DrawIcon(Scene scene, Vector2 pos, float slide)
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

        public override void DrawIcon(Scene scene, Vector2 pos, float slide)
        {
            Element.Symbol.DrawIcon(scene, pos, slide);
            base.DrawIcon(scene, pos, slide);
        }
    }

    class SymbolBar : Symbol
    {
        public SymbolBar(SpriteReference sprite) : base(sprite)
        {
        }

        public override void DrawIcon(Scene scene, Vector2 pos, float slide)
        {
            int amount = (int)Math.Round(slide * Sprite.Height);
            if (amount < 0)
                amount = 0;
            if (amount > Sprite.Height)
                amount = Sprite.Height;
            if (amount <= 0 && slide > 0)
                amount = 1;
            scene.SpriteBatch.Draw(Sprite.Texture, pos, new Rectangle(Sprite.Width * 1, 0, Sprite.Width, Sprite.Height), Color.White);
            scene.SpriteBatch.Draw(Sprite.Texture, pos + new Vector2(0, Sprite.Height - amount), new Rectangle(Sprite.Width * 0, Sprite.Height - amount, Sprite.Width, amount), Color.White);
        }
    }
}
