using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class Menu
    {
        public int Ticks;
        public virtual bool ShouldClose
        {
            get;
            set;
        }

        public void Close()
        {
            ShouldClose = true;
        }

        public virtual void HandleInput(SceneGame scene)
        {
            Ticks++;
        }

        public virtual bool IsMouseOver(int x, int y)
        {
            return false;
        }

        protected void DrawLabelledUI(SceneGame scene, SpriteReference sprite, Rectangle rectInterior, string label)
        {
            Rectangle rectExterior = new Rectangle(rectInterior.X, rectInterior.Y - 20, rectInterior.Width, 16);
            scene.DrawUI(sprite, rectInterior, Color.White);
            if (!string.IsNullOrWhiteSpace(label))
            {
                scene.DrawUI(sprite, rectExterior, Color.White);
                scene.DrawText(label, new Vector2(rectExterior.X, rectExterior.Y), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true).SetConstraints(rectExterior.Width - 16, rectExterior.Height));
            }
        }

        public abstract void Draw(SceneGame scene);
    }

    class PlayerUI : Menu
    {
        SceneGame Scene;
        Menu SubMenu;

        public Creature Player => Scene.Player;

        public override bool ShouldClose
        {
            get
            {
                return false;
            }
            set
            {
                //NOOP;
            }
        }

        public PlayerUI(SceneGame scene)
        {
            Scene = scene;
        }

        public override bool IsMouseOver(int x, int y)
        {
            if (SubMenu != null)
                return SubMenu.IsMouseOver(x, y);
            return base.IsMouseOver(x, y);
        }

        public override void HandleInput(SceneGame scene)
        {
            base.HandleInput(scene);

            InputTwinState state = Scene.InputState;

            if (SubMenu != null)
            {
                SubMenu.HandleInput(scene);
                if (SubMenu.ShouldClose)
                    SubMenu = null;
                return;
            }

            if (state.IsKeyPressed(Keys.W, 15, 5))
            {
                if (Player.Facing != Facing.North)
                {
                    Player.Facing = Facing.North;
                }
                else
                {
                    Player.ResetTurn();
                    Player.CurrentAction = Scheduler.Instance.RunAndWait(Player.RoutineMove(0, -1));
                }
            }
            if (state.IsKeyPressed(Keys.S, 15, 5))
            {
                if (Player.Facing != Facing.South)
                {
                    Player.Facing = Facing.South;
                }
                else
                {
                    Player.ResetTurn();
                    Player.CurrentAction = Scheduler.Instance.RunAndWait(Player.RoutineMove(0, 1));
                }
            }
            if (state.IsKeyPressed(Keys.A, 15, 5))
            {
                if (Player.Facing != Facing.West)
                {
                    Player.Facing = Facing.West;
                }
                else
                {
                    Player.ResetTurn();
                    Player.CurrentAction = Scheduler.Instance.RunAndWait(Player.RoutineMove(-1, 0));
                }
            }
            if (state.IsKeyPressed(Keys.D, 15, 5))
            {
                if (Player.Facing != Facing.East)
                {
                    Player.Facing = Facing.East;
                }
                else
                {
                    Player.ResetTurn();
                    Player.CurrentAction = Scheduler.Instance.RunAndWait(Player.RoutineMove(1, 0));
                }
            }
            if (state.IsKeyPressed(Keys.Space))
            {
                var offset = Player.Facing.ToOffset();
                Player.ResetTurn();
                Scene.Wait = Player.CurrentAction = Scheduler.Instance.RunAndWait(Player.RoutineAttack(offset.X, offset.Y));
            }
            if (state.IsKeyPressed(Keys.Enter))
            {
                MenuTextSelection selection = new MenuTextSelection(String.Empty, new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height * 3 / 4), 256, 8);

                Tile tile = Player.Tile;
                tile.AddActions(Player, selection);
                foreach (Tile neighbor in tile.GetAdjacentNeighbors())
                    neighbor.AddActions(Player, selection);
                selection.Add(new ActAction("Inventory", () =>
                {
                    selection.Close();
                    Open(new MenuInventory(Scene, Player));
                }));
                selection.AddDefault(new ActAction("Cancel", () => selection.Close()));

                Open(selection);
            }
        }

        public override void Draw(SceneGame scene)
        {
            if (SubMenu != null)
            {
                SubMenu.Draw(scene);
            }
        }

        public void Open(Menu menu)
        {
            SubMenu = menu;
        }
    }

    class MenuInventory : Menu
    {
        class ItemList : MenuItemSelection
        {
            MenuInventory Parent;
            public Creature Player => Parent.Player;
            public Item SelectedItem => Selection < SelectionCount ? Items[Selection] : null;

            public ItemList(MenuInventory parent, Vector2 position, int width, int scrollHeight) : base("Inventory", position, width, scrollHeight)
            {
                Parent = parent;
                Reset();
            }

            public void Reset()
            {
                Items.Clear();
                Items.AddRange(Player.GetEffects<EffectItemInventory>().Select(effect => effect.Item));
            }

            public override void HandleInput(SceneGame scene)
            {
                if (scene.InputState.IsKeyPressed(Keys.Escape))
                    Close();
                base.HandleInput(scene);
            }

            public override void Select(int selection)
            {
                Item item = Items[selection];
                Parent.OpenItemActionMenu(item);
            }

            public override void Draw(SceneGame scene)
            {
                base.Draw(scene);
            }

            public override void DrawLine(SceneGame scene, Vector2 linePos, int e)
            {
                Item item = Items[e];
                SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
                if (Selection == e)
                    scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
                scene.DrawText($"{Game.FormatIcon(item)}{item.InventoryName}", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
            }
        }

        SceneGame Scene;
        Creature Player;
        ItemList ItemMenu;
        MenuTextSelection ItemActionMenu;

        public MenuInventory(SceneGame scene, Creature player)
        {
            Scene = scene;
            Player = player;
            ItemMenu = new ItemList(this, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height / 2), 256, 20);
        }

        public override bool IsMouseOver(int x, int y)
        {
            if (ItemMenu != null && ItemMenu.IsMouseOver(x, y))
                return true;
            if (ItemActionMenu != null && ItemActionMenu.IsMouseOver(x, y))
                return true;
            int widthDescription = 256;
            int heightDescription = 20 * 16;
            Rectangle rectDescription = new Rectangle(Scene.Viewport.Width * 3 / 4 - widthDescription / 2, Scene.Viewport.Height / 2 - heightDescription / 2, widthDescription, heightDescription);
            if (rectDescription.Contains(x, y))
                return true;
            return base.IsMouseOver(x, y);
        }

        public void OpenItemActionMenu(Item item)
        {
            ItemActionMenu = new MenuTextSelection($"{Game.FormatIcon(item)}{item.Name}", new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height / 2), 128, 6);
            ItemActionMenu.Add(new ActAction("Throw Away", () =>
            {
                item.MoveTo(Player.Tile);
                ItemActionMenu.Close();
                ItemMenu.Reset();
            }));
            ItemActionMenu.AddDefault(new ActAction("Cancel", () =>
            {
                ItemActionMenu.Close();
            }));
        }

        public override void HandleInput(SceneGame scene)
        {
            InputTwinState state = scene.InputState;

            if (ItemActionMenu != null)
            {
                ItemActionMenu.HandleInput(scene);
                if (ItemActionMenu.ShouldClose)
                    ItemActionMenu = null;
            }
            else
            {
                ItemMenu.HandleInput(scene);
                if (ItemMenu.ShouldClose)
                    Close();
            }
        }

        private string GetDescription(Item item)
        {
            string description = string.Empty;
            if (item != null)
                item.AddStatBlock(ref description);
            return description;
        }

        public override void Draw(SceneGame scene)
        {
            ItemMenu.Draw(scene);
            if (ItemActionMenu != null)
                ItemActionMenu.Draw(scene);

            SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int widthDescription = 256;
            int heightDescription = 20 * 16;
            Rectangle rectDescription = new Rectangle(Scene.Viewport.Width * 3 / 4 - widthDescription / 2, Scene.Viewport.Height / 2 - heightDescription / 2, widthDescription, heightDescription);
            Item item = ItemMenu.SelectedItem;
            DrawLabelledUI(scene, textbox, rectDescription, item != null ? $"{Game.FormatIcon(item)}{item.Name}\n" : $"No Item");
            string desc = GetDescription(item);
            scene.DrawText(desc, new Vector2(rectDescription.X, rectDescription.Y), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(rectDescription));
        }
    }

    abstract class MenuAct : Menu
    {
        public string Name;

        public abstract int SelectionCount
        {
            get;
        }
        public int Selection;
        public int Scroll;
        public int ScrollHeight;

        public int DefaultSelection = -1;

        public Vector2 Position;
        public int Width;
        public int Height => ScrollHeight * 16;

        public MenuAct(string name, Vector2 position, int width, int scrollHeight)
        {
            Name = name;
            Position = position;
            Width = width;
            ScrollHeight = scrollHeight;
        }

        public override bool IsMouseOver(int x, int y)
        {
            return new Rectangle((int)Position.X - Width / 2, (int)Position.Y - Height / 2, Width, Height).Contains(x, y);
        }

        public abstract void Select(int selection);

        public override void HandleInput(SceneGame scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Enter) && Selection < SelectionCount)
                Select(Selection);
            if (scene.InputState.IsKeyPressed(Keys.Escape) && DefaultSelection >= 0)
                Select(DefaultSelection);
            if (scene.InputState.IsKeyPressed(Keys.W, 15, 5))
                Selection--;
            if (scene.InputState.IsKeyPressed(Keys.S, 15, 5))
                Selection++;
            Selection = SelectionCount <= 0 ? 0 : (Selection + SelectionCount) % SelectionCount;
            if (Selection < Scroll)
                Scroll = Selection;
            if (Selection >= Scroll + ScrollHeight)
                Scroll = Math.Max(Selection - ScrollHeight - 1, 0);
            base.HandleInput(scene);
        }

        public override void Draw(SceneGame scene)
        {
            SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int x = (int)Position.X - Width / 2;
            int y = (int)Position.Y - Height / 2;
            float openCoeff = Math.Min(Ticks / 7f, 1f);
            float openResize = MathHelper.Lerp(-0.5f, 0.0f, openCoeff);
            Rectangle rect = new Rectangle(x, y, Width, Height);
            rect.Inflate(rect.Width * openResize, rect.Height * openResize);
            if (openCoeff > 0)
                DrawLabelledUI(scene, textbox, rect, openCoeff >= 1 ? Name : string.Empty);
            if (openCoeff >= 1)
                for (int i = 0; i < ScrollHeight; i++)
                {
                    int e = Scroll + i;
                    if (e < SelectionCount)
                        DrawLine(scene, new Vector2(x, y + i * 16), e);
                }
        }

        public abstract void DrawLine(SceneGame scene, Vector2 linePos, int e);
    }

    class ActAction
    {
        public string Name;
        public Action Action;

        public ActAction(string name, Action action)
        {
            Name = name;
            Action = action;
        }
    }

    class MenuTextSelection : MenuAct
    {
        List<ActAction> Actions = new List<ActAction>();

        public override int SelectionCount => Actions.Count;

        public MenuTextSelection(string name, Vector2 position, int width, int scrollHeight) : base(name, position, width, scrollHeight)
        {
        }

        public void Add(ActAction action)
        {
            Actions.Add(action);
        }

        public void AddDefault(ActAction action)
        {
            DefaultSelection = Actions.Count;
            Add(action);
        }

        public override void Select(int selection)
        {
            Actions[selection].Action();
        }

        public override void DrawLine(SceneGame scene, Vector2 linePos, int e)
        {
            ActAction action = Actions[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
            scene.DrawText(action.Name, linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
        }
    }

    class MenuItemSelection : MenuAct
    {
        protected List<Item> Items = new List<Item>();

        public override int SelectionCount => Items.Count;

        public MenuItemSelection(string name, Vector2 position, int width, int scrollHeight) : base(name, position, width, scrollHeight)
        {
        }

        public override void Select(int selection)
        {

        }

        public override void DrawLine(SceneGame scene, Vector2 linePos, int e)
        {
            Item item = Items[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
            scene.DrawText($"{Game.FormatIcon(item)}{item.InventoryName}", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
        }
    }
}
