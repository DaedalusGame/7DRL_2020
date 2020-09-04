using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Menus
{
    class PlayerUI : Menu
    {
        public SceneGame Scene;
        Menu SubMenu;
        SkillBox SkillInfo;
        public BossWarning BossWarning;

        Slider GameOver = new Slider(100);
        MenuTextSelection GameOverMenu;

        public Creature Player => Scene.Player;
        public Turn Turn => Scene.Turn;
        public TurnTaker TurnTaker => Scene.Turn.TurnTaker;

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

        public override void Update(Scene scene)
        {
            if (Player.Dead && Player.CurrentActions.Done)
            {
                GameOver += 1;
            }

            if (GameOverMenu != null)
                GameOverMenu.Update(scene);

            if (BossWarning != null)
            {
                BossWarning.HandleInput(scene);
                BossWarning.Update(scene);
                if (BossWarning.ShouldClose)
                    BossWarning = null;
            }

            if (SkillInfo != null)
            {
                SkillInfo.Update(scene);
                if (SkillInfo.ShouldClose)
                    SkillInfo = null;
            }
            else if (Scene.CurrentSkill != null)
            {
                SkillInfo = new SkillBox(Scene, new Vector2(Scene.Viewport.Width / 2, 64), 332, 40);
            }

            if (SubMenu != null)
            {
                SubMenu.Update(scene);
            }

            base.Update(scene);
        }

        private void TryMove(Creature player, Facing facing)
        {
            Point offset = facing.ToOffset();
            int dx = offset.X;
            int dy = offset.Y;
            var frontier = player.Mask.GetFrontier(dx, dy).Select(o => player.Tile.GetNeighbor(o.X, o.Y));
            if (frontier.All(front => !front.Solid && !front.Creatures.Any()))
            {
                TakeAction(Scheduler.Instance.RunAndWait(Player.RoutineMove(dx, dy)), false);
            }
            else if (frontier.Any(front => front is IMineable) && player.GetStat(Stat.MiningLevel) > 0)
            {
                TakeAction(Scheduler.Instance.RunAndWait(Player.RoutineAttack(dx, dy, Creature.MeleeAttack)), true);
            }
        }

        public void TakeAction(Wait wait, bool shouldBlock)
        {
            Player.CurrentActions.Add(wait);
            if (shouldBlock)
                Scene.Wait.Add(wait);
            Turn.End();
        }

        public override void HandleInput(Scene scene)
        {
            base.HandleInput(scene);

            InputTwinState state = Scene.InputState;

            if (GameOverMenu != null)
                GameOverMenu.HandleInput(scene);

            if (Player.Dead && Player.CurrentActions.Done)
            {
                if (GameOver.Done && GameOverMenu == null)
                {
                    GameOverMenu = new MenuTextSelection(string.Empty, new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height * 3 / 4), 300, 2);
                    /*GameOverMenu.Add(new ActAction("Restart", "Start over.", () =>
                    {
                        Scene.Restart();
                    }));*/
                    GameOverMenu.Add(new ActAction("Return to Title", "Return to Titlescreen.", () =>
                    {
                        Scene.ReturnToTitle();
                    }));
                    GameOverMenu.Add(new ActAction("Quit", "Quit to Desktop.", () =>
                    {
                        Scene.Quit();
                    }));
                }
                return;
            }

            if (SubMenu != null)
            {
                SubMenu.HandleInput(scene);
                if (SubMenu.ShouldClose)
                    SubMenu = null;
                return;
            }

            if (Player.CurrentActions.Done)
            {
                if (state.IsKeyPressed(Keys.W, 15, 5))
                {
                    if (Player.Facing != Facing.North)
                    {
                        Player.Facing = Facing.North;
                        return;
                    }
                    else
                    {
                        TryMove(Player, Facing.North);
                        return;
                    }
                }
                if (state.IsKeyPressed(Keys.S, 15, 5))
                {
                    if (Player.Facing != Facing.South)
                    {
                        Player.Facing = Facing.South;
                        return;
                    }
                    else
                    {
                        TryMove(Player, Facing.South);
                        return;
                    }
                }
                if (state.IsKeyPressed(Keys.A, 15, 5))
                {
                    if (Player.Facing != Facing.West)
                    {
                        Player.Facing = Facing.West;
                        return;
                    }
                    else
                    {
                        TryMove(Player, Facing.West);
                        return;
                    }
                }
                if (state.IsKeyPressed(Keys.D, 15, 5))
                {
                    if (Player.Facing != Facing.East)
                    {
                        Player.Facing = Facing.East;
                        return;
                    }
                    else
                    {
                        TryMove(Player, Facing.East);
                        return;
                    }
                }
                if (state.IsKeyPressed(Keys.R) && Player.HasFlag(Stat.SwapItem))
                {
                    var mainhand = Player.EquipMainhand;
                    var offhand = Player.EquipOffhand;
                    Player.Unequip(EquipSlot.Mainhand);
                    Player.Unequip(EquipSlot.Offhand);
                    if (offhand != null)
                        Player.Equip(offhand, EquipSlot.Mainhand);
                    if (mainhand != null)
                        Player.Equip(mainhand, EquipSlot.Offhand);
                    return;
                }
                if (state.IsKeyPressed(Keys.Space))
                {
                    var offset = Player.Facing.ToOffset();
                    TakeAction(Scheduler.Instance.RunAndWait(Player.RoutineAttack(offset.X, offset.Y, Creature.MeleeAttack)), true);
                    return;
                }
                if (state.IsKeyPressed(Keys.LeftControl) && Player.EquipQuiver is ToolArrow arrow && arrow.Durability > 0)
                {
                    var offset = Player.Facing.ToOffset();
                    TakeAction(Scheduler.Instance.RunAndWait(Player.RoutineShootArrow(offset.X, offset.Y)), true);
                    return;
                }
            }
            if (state.IsKeyPressed(Keys.Enter))
            {
                MenuTextSelection selection = new MenuTextSelection(String.Empty, new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height * 3 / 4), 256, 8);

                Tile tile = Player.Tile;
                tile.AddActions(this, Player, selection);
                foreach (Tile neighbor in tile.GetAdjacentNeighbors())
                    neighbor.AddActions(this, Player, selection);
                selection.Add(new ActAction("Status", "Shows your stats.", () =>
                {
                    selection.Close();
                    Open(new MenuStatus(this, Player));
                }));
                selection.Add(new ActAction("Inventory", "Opens your inventory.", () =>
                {
                    selection.Close();
                    Open(new MenuInventory(this, Player));
                }));
                selection.Add(new ActAction("Save", "Saves the game.", () =>
                {
                    selection.Close();
                    Scene.Save();
                }));
                selection.Add(new ActAction("Return to Title", "Returns to Titlescreen.", () =>
                {
                    selection.Close();
                    //TODO: Require confirmation
                    Scene.ReturnToTitle();
                }));
                selection.AddDefault(new ActAction("Cancel", "Closes this menu.", () => selection.Close()));

                Open(selection);
            }
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (SubMenu != null)
                SubMenu.PreDraw(scene);

            if (SkillInfo != null)
                SkillInfo.PreDraw(scene);

            if (BossWarning != null)
                BossWarning.PreDraw(scene);

            if (GameOverMenu != null)
                GameOverMenu.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            SceneGame sceneGame = (SceneGame)scene;

            DrawSlot(sceneGame, new Vector2(48, scene.Viewport.Height - 48), "Offhand", Player.EquipOffhand);
            DrawSlot(sceneGame, new Vector2(96, scene.Viewport.Height - 64), "Body", Player.EquipBody);
            DrawSlot(sceneGame, new Vector2(144, scene.Viewport.Height - 48), "Mainhand", Player.EquipMainhand);
            DrawSlot(sceneGame, new Vector2(48, scene.Viewport.Height - 48 - 48), "Quiver", Player.EquipQuiver);

            if (SubMenu != null)
            {
                SubMenu.Draw(scene);
            }

            if (SkillInfo != null)
            {
                SkillInfo.Draw(scene);
            }

            if (BossWarning != null)
            {
                BossWarning.Draw(scene);
            }

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(0, 0, scene.Viewport.Width, scene.Viewport.Height), new Color(0, 0, 0, GameOver.Slide * 0.5f));

            if (GameOverMenu != null)
                GameOverMenu.Draw(scene);
        }

        public void DrawSlot(SceneGame scene, Vector2 position, string name, Item item)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            SpriteReference uiSlot = SpriteLoader.Instance.AddSprite("content/ui_slot");
            scene.DrawUI(uiSlot, new Rectangle(x - 16, y - 16, 32, 32), Color.White);
            scene.DrawText(Game.ConvertToSmallPixelText(name), position + new Vector2(0, 20 - 8), Alignment.Center, new TextParameters().SetColor(Color.White, Color.Black));
            if (item != null)
            {
                scene.PushSpriteBatch(transform: Matrix.CreateScale(2, 2, 1));
                item.DrawIcon(scene, position / 2);
                scene.PopSpriteBatch();
            }
            if (item is ToolCore tool)
            {
                Color color;
                double ratio = tool.Durability / tool.DurabilityMax;
                if (ratio >= 1)
                    color = new Color(128, 255, 255);
                else if (ratio >= 0.2)
                    color = Color.White;
                else if (ratio >= 0.1)
                    color = Color.Orange;
                else if (ratio > 0.0)
                    color = Color.Red;
                else
                    color = Color.Gray;
                string durability = tool.Durability.ToString();
                scene.DrawText(Game.ConvertToSmallPixelText(durability), position + new Vector2(0, 0), Alignment.Center, new TextParameters().SetColor(color, Color.Black));
            }
        }

        public void Open(Menu menu)
        {
            SubMenu = menu;
        }
    }

    interface IInventory
    {
        Creature Holder
        {
            get;
        }

        void SelectItem(Item item);
    }

    class InventoryItemList : MenuItemSelection
    {
        IInventory Parent;
        public IEffectHolder Holder => Parent.Holder;
        public Item SelectedItem => Selection < SelectionCount ? Items[Selection] : null;
        public Func<Item, bool> Filter = (item) => true;

        public InventoryItemList(IInventory parent, Vector2 position, int width, int scrollHeight) : base("Inventory", position, width, scrollHeight)
        {
            Parent = parent;
            Reset();
        }

        public void Reset()
        {
            Items.Clear();
            Items.AddRange(Holder.GetInventory());
        }

        public override void HandleInput(Scene scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Escape))
                Close();
            base.HandleInput(scene);
        }

        public override void Select(int selection)
        {
            Item item = Items[selection];
            if (Filter(item))
                Parent.SelectItem(item);
        }

        public override void Draw(Scene scene)
        {
            base.Draw(scene);
        }

        public override void DrawLine(Scene scene, Vector2 linePos, int e)
        {
            Item item = Items[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
            scene.DrawText($"{Game.FormatIcon(item)}{item.InventoryName}", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Filter(item) ? Color.White : Color.Gray, Color.Black));
        }
    }

    class MenuAnvil : Menu
    {
        class MenuCraftingSelection : MenuAct, IInventory
        {
            class PartAction : ActAction
            {
                MenuCraftingSelection Menu;
                public int PartIndex;

                public override string Name
                {
                    get
                    {
                        IOre ore = Menu.Parts[PartIndex] as IOre;
                        Material material = ore?.Material;
                        return $"{material?.Name ?? "No"} {Menu.PartTypes.GetName(PartIndex)}";
                    }
                    set
                    {
                        //NOOP
                    }
                }
                public override string Description
                {
                    get
                    {
                        return "";
                    }
                    set
                    {
                        //NOOP
                    }
                }

                public PartAction(MenuCraftingSelection menu, int partIndex) : base(null, null, null, null)
                {
                    Menu = menu;
                    PartIndex = partIndex;
                    Action = Select;
                    Enabled = IsEnabled;
                }

                private void Select()
                {
                    Menu.SelectPart(PartIndex);
                }

                private bool IsEnabled()
                {
                    return true;
                }
            }

            class RenameAction : ActAction
            {
                MenuCraftingSelection Menu;

                public override string Name
                {
                    get
                    {
                        if (string.IsNullOrEmpty(Menu.Nickname))
                            return "No Nickname";
                        else
                            return $"\"{Menu.Nickname}\"";
                    }
                    set
                    {
                        //NOOP
                    }
                }

                public RenameAction(MenuCraftingSelection menu) : base(null, null, null, null)
                {
                    Menu = menu;
                    Action = Select;
                    Enabled = IsEnabled;
                }

                private void Select()
                {
                    Menu.NameInputMenu = new NameInput("Nickname", "Enter a nickname:", new Vector2(Menu.Scene.Viewport.Width / 2, Menu.Scene.Viewport.Height / 2), 200, Menu.Nickname);
                }

                private bool IsEnabled()
                {
                    return true;
                }
            }

            MenuAnvil MenuAnvil;
            Anvil Anvil => MenuAnvil.Anvil;
            SceneGame Scene => MenuAnvil.Scene;

            List<ActAction> Actions = new List<ActAction>();

            public override int SelectionCount => Actions.Count;

            public Creature Holder => MenuAnvil.Holder;

            public Item[] Parts;
            public int CurrentPart;
            public PartType[] PartTypes;
            public Func<Material[], ToolCore> Create;
            public Func<Material[], string> GetBaseName;
            public ToolCore Result;
            public string Nickname = string.Empty;

            InventoryItemList ItemMenu;
            InfoBox InfoWindow;
            NameInput NameInputMenu;

            public MenuCraftingSelection(MenuAnvil menuAnvil, Vector2 position, string blueprintName, PartType[] parts) : base(blueprintName, position, 256, 8)
            {
                MenuAnvil = menuAnvil;
                PartTypes = parts;
                Parts = new Item[parts.Length];
                InfoWindow = new InfoBox(() => "Preview", () => GetResultDescription(), new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20 * 16);
                DefaultSelection = SelectionCount - 1;

                Actions.Add(new RenameAction(this));
                for (int i = 0; i < parts.Length; i++)
                {
                    Actions.Add(new PartAction(this, i));
                }
                Actions.Add(new ActAction("Build", "", () => {
                    Result.MoveTo(Anvil);
                    foreach (var part in Parts)
                        part.Destroy();
                    Close();
                }, () => Result != null));
                Actions.Add(new ActAction("Cancel", "", () => {
                    foreach (Item item in Parts)
                    {
                        if (item != null)
                            Holder.Pickup(item);
                    }
                    Close();
                }));
                DefaultSelection = Actions.Count - 1;
            }

            private string GetResultDescription()
            {
                if (Result != null)
                {
                    string statBlock = string.Empty;
                    statBlock += $"{Game.FormatIcon(Result)}{Game.FORMAT_BOLD}{Result.Name}{Game.FORMAT_BOLD}\n";
                    Result.AddStatBlock(ref statBlock);
                    return statBlock;
                }
                else
                {
                    return "Invalid Part Combination";
                }
            }

            public void Reset()
            {
                if (Result != null)
                {
                    Result.Destroy();
                    Result = null;
                }
                if (Parts.All(x => x is IOre))
                {
                    Result = Create(Parts.OfType<IOre>().Select(x => x.Material).ToArray());
                    Result.AddName(GetNickname());
                }
            }

            public override bool IsMouseOver(int x, int y)
            {
                if (ItemMenu != null && ItemMenu.IsMouseOver(x, y))
                    return true;
                if (InfoWindow != null && InfoWindow.IsMouseOver(x, y))
                    return true;
                return base.IsMouseOver(x, y);
            }

            public override void Update(Scene scene)
            {
                base.Update(scene);
                InfoWindow.Update(scene);
                if (ItemMenu != null)
                {
                    ItemMenu.Update(scene);
                }
                if (NameInputMenu != null)
                {
                    NameInputMenu.Update(scene);
                }
            }

            public override void HandleInput(Scene scene)
            {
                InfoWindow.HandleInput(scene);
                if (NameInputMenu != null)
                {
                    NameInputMenu.HandleInput(scene);
                    if (NameInputMenu.ShouldClose)
                    {
                        if (NameInputMenu.HasResult)
                        {
                            Nickname = NameInputMenu.NewString.Trim();
                            Reset();
                        }
                        NameInputMenu = null;
                    }
                }
                else if (ItemMenu != null)
                {
                    ItemMenu.HandleInput(scene);
                    if (ItemMenu.ShouldClose)
                        ItemMenu = null;
                }
                else
                {
                    base.HandleInput(scene);
                }
            }

            public override void PreDraw(Scene scene)
            {
                base.PreDraw(scene);

                if (ItemMenu != null)
                    ItemMenu.PreDraw(scene);
                if (InfoWindow != null)
                    InfoWindow.PreDraw(scene);
                if (NameInputMenu != null)
                    NameInputMenu.PreDraw(scene);
            }

            public override void Draw(Scene scene)
            {
                base.Draw(scene);

                if (ItemMenu != null)
                    ItemMenu.Draw(scene);
                InfoWindow.Draw(scene);
                if (NameInputMenu != null)
                    NameInputMenu.Draw(scene);
            }

            public override void DrawLine(Scene scene, Vector2 linePos, int e)
            {
                SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
                if (Selection == e)
                    scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
                ActAction action = Actions[e];
                Color color = Color.White;
                int offset = 16;
                if (!action.Enabled())
                    color = Color.Gray;
                if (action is PartAction partAction)
                {
                    IOre ore = Parts[partAction.PartIndex] as IOre;
                    Material material = ore?.Material;
                    if (material != null)
                    {
                        var partSprite = PartTypes.GetSprite(partAction.PartIndex, material);
                        scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
                        {
                            scene.SetupColorMatrix(material.ColorTransform, matrix, projection);
                        });
                        scene.DrawSprite(partSprite, 0, linePos + new Vector2(16, 0), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                        scene.PopSpriteBatch();
                    }
                    offset = 32;
                }
                scene.DrawText(action.Name, linePos + new Vector2(offset, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(color, Color.Black));
            }

            public override void Select(int selection)
            {
                if (Actions[selection].Enabled())
                    Actions[selection].Action();
            }

            public void SelectPart(int part)
            {
                CurrentPart = part;
                ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height * 3 / 4), 256, 8)
                {
                    Filter = (item) => item is IOre ore && ore.CanUseInAnvil(PartTypes[CurrentPart]),
                };
            }

            private string GetNickname()
            {
                if (!string.IsNullOrEmpty(Nickname))
                    return Nickname;
                else
                    return GetBaseName(Parts.OfType<IOre>().Select(x => x.Material).ToArray());
            }

            public void PickEmptyPart()
            {
                for (int i = 0; i < Actions.Count; i++)
                {
                    var action = Actions[i];
                    if (action is PartAction partAction && Parts[partAction.PartIndex] == null)
                    {
                        Selection = i;
                        CurrentPart = partAction.PartIndex;
                        return;
                    }
                }
            }

            public void SelectItem(Item item)
            {
                if (Parts[CurrentPart] != null) //Return existing
                {
                    Holder.Pickup(Parts[CurrentPart]);
                    Parts[CurrentPart] = null;
                }
                if (Parts[CurrentPart] == null)
                {
                    Item partMaterial = null;
                    if (item is Ore ore)
                    {
                        partMaterial = ore.Split(200);
                    }
                    if (item is Ingot ingot)
                    {
                        partMaterial = ingot.Split(1);
                    }
                    if (item is OreItem oreItem)
                    {
                        partMaterial = oreItem.Split(1);
                    }
                    if (partMaterial != null)
                    {
                        Parts[CurrentPart] = partMaterial;
                        Anvil.Container.Add(partMaterial, false);
                    }
                    PickEmptyPart();
                    Reset();
                }
                ItemMenu.Reset();
            }
        }

        PlayerUI UI;
        SceneGame Scene => UI.Scene;
        Creature Holder;
        Anvil Anvil;

        MenuTextSelection BlueprintMenu;
        MenuCraftingSelection CraftingMenu;

        public MenuAnvil(PlayerUI ui, Creature holder, Anvil anvil)
        {
            UI = ui;
            Holder = holder;
            Anvil = anvil;
            BlueprintMenu = new MenuTextSelection("Anvil", new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height * 1 / 4), 256, 8);
            BlueprintMenu.Add(new ActAction("Blade", "Blades do damage to enemies.", () =>
            {
                OpenBlueprintMenu("Blade", ToolBlade.Parts, (materials) => ToolBlade.Create(Scene, materials), ToolBlade.GetNickname);
            }));
            BlueprintMenu.Add(new ActAction("Adze", "Adzes can be used to mine blocks.", () =>
            {
                OpenBlueprintMenu("Adze", ToolAdze.Parts, (materials) => ToolAdze.Create(Scene, materials), ToolAdze.GetNickname);
            }));
            BlueprintMenu.Add(new ActAction("Plate", "Plates can be worn as armor or used as shields.", () =>
            {
                OpenBlueprintMenu("Plate", ToolPlate.Parts, (materials) => ToolPlate.Create(Scene, materials), ToolPlate.GetNickname);
            }));
            BlueprintMenu.Add(new ActAction("Arrow", "Arrows can be fired at enemies.", () =>
            {
                OpenBlueprintMenu("Arrow", ToolArrow.Parts, (materials) => ToolArrow.Create(Scene, materials), ToolArrow.GetNickname);
            }));
            BlueprintMenu.AddDefault(new ActAction("Cancel", "Closes this menu.", () =>
            {
                BlueprintMenu.Close();
            }));
        }

        private void OpenBlueprintMenu(string name, PartType[] parts, Func<Material[], ToolCore> create, Func<Material[], string> getNickname)
        {
            CraftingMenu = new MenuCraftingSelection(this, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height * 2 / 4), name, parts)
            {
                Create = create,
                GetBaseName = getNickname,
            };
        }

        public override bool IsMouseOver(int x, int y)
        {
            if (BlueprintMenu != null && BlueprintMenu.IsMouseOver(x, y))
                return true;
            if (CraftingMenu != null && CraftingMenu.IsMouseOver(x, y))
                return true;
            return base.IsMouseOver(x, y);
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);

            if (CraftingMenu != null)
            {
                CraftingMenu.Update(scene);
            }
            if (BlueprintMenu != null)
            {
                BlueprintMenu.Update(scene);
            }
        }

        public override void HandleInput(Scene scene)
        {
            if (CraftingMenu != null)
            {
                CraftingMenu.HandleInput(scene);
                if (CraftingMenu.ShouldClose)
                    CraftingMenu = null;
            }
            else if (BlueprintMenu != null)
            {
                BlueprintMenu.HandleInput(scene);
                if (BlueprintMenu.ShouldClose)
                    Close();
            }
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (BlueprintMenu != null)
                BlueprintMenu.PreDraw(scene);
            if (CraftingMenu != null)
                CraftingMenu.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            if (BlueprintMenu != null)
                BlueprintMenu.Draw(scene);
            if (CraftingMenu != null)
                CraftingMenu.Draw(scene);
        }
    }

    class MenuSmelter : Menu, IInventory
    {
        enum SmelterSelection
        {
            Ore,
            Fuel,
            Empty,
            Cancel,
        }

        PlayerUI UI;
        SceneGame Scene => UI.Scene;
        public Creature Holder
        {
            get;
            set;
        }
        Smelter Smelter;
        SmelterSelection Selection;
        InventoryItemList ItemMenu;
        MenuTextSelection ActionMenu;
        MenuTextSelection SmelterMenu;
        InfoBox SmelterInfo;

        public MenuSmelter(PlayerUI ui, Creature holder, Smelter smelter)
        {
            UI = ui;
            Holder = holder;
            Smelter = smelter;

            SmelterMenu = new MenuTextSelection("", new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height * 3 / 4), 256, 6);
            SmelterMenu.Add(new ActAction("Add Ore", "Add ore to be smelted.", () => {
                Selection = SmelterSelection.Ore;
                ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20)
                {
                    Filter = (item) => item is IOre,
                };
            }));
            SmelterMenu.Add(new ActAction("Add Fuel", "Add ore to be smelted.", () => {
                Selection = SmelterSelection.Fuel;
                ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20)
                {
                    Filter = (item) => item is IFuel fuel && fuel.FuelTemperature > 0,
                };
            }));
            SmelterMenu.Add(new ActAction("Empty", "Add ore to be smelted.", () => {
                Selection = SmelterSelection.Empty;
                Smelter.Empty();
            }));
            SmelterMenu.AddDefault(new ActAction("Cancel", "", () => { SmelterMenu.Close(); }));

            SmelterInfo = new InfoBox(() => "Smelter", this.GetDescription, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height / 2), 256, 20 * 16);
        }

        private string GetDescription()
        {
            string description = String.Empty;
            Smelter.AddDescription(ref description);
            return description;
        }

        public override bool IsMouseOver(int x, int y)
        {
            if (ItemMenu != null && ItemMenu.IsMouseOver(x, y))
                return true;
            if (ActionMenu != null && ActionMenu.IsMouseOver(x, y))
                return true;
            if (SmelterMenu != null && SmelterMenu.IsMouseOver(x, y))
                return true;
            return base.IsMouseOver(x, y);
        }

        public override void Update(Scene scene)
        {
            if (ActionMenu != null)
            {
                ActionMenu.Update(scene);
            }
            if (ItemMenu != null)
            {
                ItemMenu.Update(scene);
            }
            if (SmelterMenu != null)
            {
                SmelterMenu.Update(scene);
            }
            if (SmelterInfo != null)
            {
                SmelterInfo.Update(scene);
            }
        }

        public override void HandleInput(Scene scene)
        {
            InputTwinState state = scene.InputState;

            if (SmelterInfo != null)
                SmelterInfo.HandleInput(scene);

            if (ActionMenu != null)
            {
                ActionMenu.HandleInput(scene);
                if (ActionMenu.ShouldClose)
                    ActionMenu = null;
            }
            else if (ItemMenu != null)
            {
                ItemMenu.HandleInput(scene);
                if (ItemMenu.ShouldClose)
                    ItemMenu = null;
            }
            else if (SmelterMenu != null)
            {
                SmelterMenu.HandleInput(scene);
                if (SmelterMenu.ShouldClose)
                {
                    SmelterMenu = null;
                    Close();
                }
            }
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (ItemMenu != null)
                ItemMenu.PreDraw(scene);
            if (ActionMenu != null)
                ActionMenu.PreDraw(scene);
            if (SmelterMenu != null)
                SmelterMenu.PreDraw(scene);
            if (SmelterInfo != null)
                SmelterInfo.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            if (SmelterInfo != null)
                SmelterInfo.Draw(scene);
            if (SmelterMenu != null)
                SmelterMenu.Draw(scene);
            if (ItemMenu != null)
                ItemMenu.Draw(scene);
            if (ActionMenu != null)
                ActionMenu.Draw(scene);
        }

        public void SelectItem(Item item)
        {
            switch (Selection)
            {
                case (SmelterSelection.Ore):
                    Smelter.OreContainer.Add(TakeOneIngot(item), true);
                    break;
                case (SmelterSelection.Fuel):
                    Smelter.FuelContainer.Add(TakeOneIngot(item), true);
                    break;
            }
            ItemMenu.Reset();
        }

        private Item TakeOneIngot(Item item)
        {
            Item partMaterial = null;
            if (item is Ore ore)
            {
                partMaterial = ore.Split(200);
            }
            if (item is Ingot ingot)
            {
                partMaterial = ingot.Split(1);
            }
            return partMaterial;
        }
    }

    class MenuStatus : Menu
    {
        PlayerUI UI;
        SceneGame Scene => UI.Scene;
        public Creature Holder
        {
            get;
            set;
        }
        InfoBox ItemInfo;

        public MenuStatus(PlayerUI ui, Creature holder)
        {
            UI = ui;
            Holder = holder;
            string text = GetDescription(Holder);
            ItemInfo = new InfoBox(() => "Status", () => text, new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20 * 16);
        }

        public override bool IsMouseOver(int x, int y)
        {
            int widthDescription = 256;
            int heightDescription = 20 * 16;
            Rectangle rectDescription = new Rectangle(Scene.Viewport.Width * 3 / 4 - widthDescription / 2, Scene.Viewport.Height / 2 - heightDescription / 2, widthDescription, heightDescription);
            if (rectDescription.Contains(x, y))
                return true;
            return base.IsMouseOver(x, y);
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);

            ItemInfo.Update(scene);
        }

        public override void HandleInput(Scene scene)
        {
            InputTwinState state = scene.InputState;

            ItemInfo.HandleInput(scene);
            if (ItemInfo.ShouldClose)
                this.ShouldClose = true;
        }

        private string GetName(Item item)
        {
            return item != null ? $"{Game.FormatIcon(item)}{item.Name}\n" : $"No Item";
        }

        private string GetDescription(Creature creature)
        {
            string description = string.Empty;
            creature.AddStatBlock(ref description);
            return description;
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (ItemInfo != null)
                ItemInfo.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            ItemInfo.Draw(scene);

            /*SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int widthDescription = 256;
            int heightDescription = 20 * 16;
            Rectangle rectDescription = new Rectangle(Scene.Viewport.Width * 3 / 4 - widthDescription / 2, Scene.Viewport.Height / 2 - heightDescription / 2, widthDescription, heightDescription);
            Item item = ItemMenu.SelectedItem;
            DrawLabelledUI(scene, textbox, rectDescription, item != null ? $"{Game.FormatIcon(item)}{item.Name}\n" : $"No Item");
            string desc = GetDescription(item);
            scene.DrawText(desc, new Vector2(rectDescription.X, rectDescription.Y), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(rectDescription));*/
        }
    }

    class MenuInventory : Menu, IInventory
    {
        PlayerUI UI;
        SceneGame Scene => UI.Scene;
        public Creature Holder
        {
            get;
            set;
        }
        InventoryItemList ItemMenu;
        MenuTextSelection ItemActionMenu;
        InfoBox ItemInfo;

        public MenuInventory(PlayerUI ui, Creature holder)
        {
            UI = ui;
            Holder = holder;
            ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height / 2), 256, 20);
            ItemInfo = new InfoBox(() => GetName(ItemMenu.SelectedItem), () => GetDescription(ItemMenu.SelectedItem), new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20 * 16);
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

        public void SelectItem(Item item)
        {
            ItemActionMenu = new MenuTextSelection($"{Game.FormatIcon(item)}{item.Name}", new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height / 2), 256, 6);
            item.AddItemActions(ItemMenu, Holder, ItemActionMenu);
            ItemActionMenu.AddDefault(new ActAction("Cancel", "Closes this menu.", () =>
            {
                ItemActionMenu.Close();
            }));
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);

            ItemInfo.Update(scene);
            if (ItemActionMenu != null)
            {
                ItemActionMenu.Update(scene);
            }
            ItemMenu.Update(scene);
        }

        public override void HandleInput(Scene scene)
        {
            InputTwinState state = scene.InputState;

            ItemInfo.HandleInput(scene);
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

        private string GetName(Item item)
        {
            return item != null ? $"{Game.FormatIcon(item)}{item.Name}\n" : $"No Item";
        }

        private string GetDescription(Item item)
        {
            string description = string.Empty;
            if (item != null)
                item.AddStatBlock(ref description);
            return description;
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            if (ItemMenu != null)
                ItemMenu.PreDraw(scene);
            if (ItemInfo != null)
                ItemInfo.PreDraw(scene);
            if (ItemActionMenu != null)
                ItemActionMenu.PreDraw(scene);
        }

        public override void Draw(Scene scene)
        {
            ItemMenu.Draw(scene);
            ItemInfo.Draw(scene);
            if (ItemActionMenu != null)
                ItemActionMenu.Draw(scene);


            /*SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int widthDescription = 256;
            int heightDescription = 20 * 16;
            Rectangle rectDescription = new Rectangle(Scene.Viewport.Width * 3 / 4 - widthDescription / 2, Scene.Viewport.Height / 2 - heightDescription / 2, widthDescription, heightDescription);
            Item item = ItemMenu.SelectedItem;
            DrawLabelledUI(scene, textbox, rectDescription, item != null ? $"{Game.FormatIcon(item)}{item.Name}\n" : $"No Item");
            string desc = GetDescription(item);
            scene.DrawText(desc, new Vector2(rectDescription.X, rectDescription.Y), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(rectDescription));*/
        }
    }

    class BossWarning : Menu
    {
        Slider OpenFrame;
        Slider WarnFrame;
        Slider NextFrame;

        List<string> Messages;
        int MessageIndex;

        bool HasMessage => MessageIndex < Messages.Count;

        int ScrollFrame;

        public BossWarning(IEnumerable<string> messages)
        {
            Messages = new List<string>();
            Messages.Add(string.Empty);
            Messages.AddRange(messages);
            OpenFrame = new Slider(20);
            WarnFrame = new Slider(40);
            NextFrame = new Slider(400);
        }

        public override void HandleInput(Scene scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Enter) && WarnFrame.Done)
            {
                NextWarning();
            }
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);

            bool isLastFrame = GetWarnSlide() <= 0.5;

            OpenFrame += 1;
            WarnFrame += 1;
            NextFrame += 1;

            if (GetWarnSlide() > 0.5 && isLastFrame)
            {
                MessageIndex++;
            }

            if (NextFrame.Done)
            {
                NextWarning();
            }

            ScrollFrame++;

            if (!HasMessage && WarnFrame.Done)
                ShouldClose = true;
        }

        private void NextWarning()
        {
            NextFrame.Time = 0;
            WarnFrame.Time = 0;
        }

        private double GetWarnSlide()
        {
            return LerpHelper.QuadraticIn(0, 1, WarnFrame.Slide);
        }

        public override void Draw(Scene scene)
        {
            SpriteReference skull = SpriteLoader.Instance.AddSprite("content/ui_skull");
            SpriteReference text = SpriteLoader.Instance.AddSprite("content/ui_warntext");

            int middle = scene.Viewport.Height * 3 / 4;
            int size = (int)(80 * OpenFrame.Slide);
            double warnSlide = GetWarnSlide();
            int distExterior = (int)(60 * Math.Min(warnSlide, 0.5) * 2);
            int distInterior = (int)(60 * Math.Max(warnSlide - 0.5, 0) * 2);
            int distDelta = distExterior - distInterior;

            int textMiddle = (int)MathHelper.Lerp(0, size / 2 + text.Height / 2, OpenFrame.Slide);
            if (HasMessage)
            {
                scene.SpriteBatch.Draw(text.Texture, new Rectangle(0, middle - textMiddle - text.Height / 2, scene.Viewport.Width, text.Height), new Rectangle(ScrollFrame, 0, scene.Viewport.Width, text.Height), Color.Red);
                scene.SpriteBatch.Draw(text.Texture, new Rectangle(0, middle + textMiddle - text.Height / 2, scene.Viewport.Width, text.Height), new Rectangle(ScrollFrame, 0, scene.Viewport.Width, text.Height), Color.Red, 0, Vector2.Zero, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally | Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipVertically, 0);
                scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(0, middle - size / 2, scene.Viewport.Width, size), Color.Red);
                //Text

                Vector2 center = new Vector2(scene.Viewport.Width / 2, middle);
                TextParameters parameters = new TextParameters().SetBold(true);
                float height = GetStringHeight(Messages[MessageIndex], parameters);
                scene.DrawText(Messages[MessageIndex], center + new Vector2(0, -height / 2), Alignment.Center, parameters);
                if (MessageIndex == 1)
                {
                    scene.DrawSprite(skull, 0, center + new Vector2(scene.Viewport.Width * 1 / 3, 0) - skull.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.Black, 0);
                    scene.DrawSprite(skull, 0, center + new Vector2(-scene.Viewport.Width * 1 / 3, 0) - skull.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.Black, 0);
                }
            }
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(0, middle - distExterior, scene.Viewport.Width, distDelta), Color.Red);
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(0, middle + distInterior, scene.Viewport.Width, distDelta), Color.Red);
        }
    }

    class SkillBox : InfoBox
    {
        SceneGame Scene;
        Skill Skill => Scene.CurrentSkill;

        public SkillBox(SceneGame scene, Vector2 position, int width, int height) : base(null, null, position, width, height)
        {
            Scene = scene;
            Name = () => Skill?.Name ?? "";
            Text = () => Skill?.Description ?? "";
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);
            if (Skill == null)
                ShouldClose = true;
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

        public override void DrawLine(Scene scene, Vector2 linePos, int e)
        {
            Item item = Items[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
            scene.DrawText($"{Game.FormatIcon(item)}{item.InventoryName}", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
        }
    }
}
