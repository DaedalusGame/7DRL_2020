using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RoguelikeEngine.Menus;

namespace RoguelikeEngine
{
    class SceneTitle : Scene
    {
        public TitleUI Menu;

        public SceneTitle(Game game) : base(game)
        {
            Menu = new TitleUI(this);
        }

        private void DrawTextures()
        {
            Menu.PreDraw(this);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTextures();

            GraphicsDevice.SetRenderTarget(null);

            PushSpriteBatch(blendState: NonPremultiplied, samplerState: SamplerState.PointWrap, projection: Projection);

            Menu.Draw(this);

            PopSpriteBatch();
        }

        public override void Update(GameTime gameTime)
        {
            Menu.Update(this);
            Menu.HandleInput(this);
        }

        public void NewGame()
        {
            Game.Scene = new SceneGame(Game);
        }

        internal void LoadGame(SaveFile selectedSaveGame)
        {
            Game.Scene = new SceneGame(Game, selectedSaveGame);
        }
    }
}
