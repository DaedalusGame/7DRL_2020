using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public virtual void Update(SceneGame scene)
        {
            Ticks++;
        }

        public virtual void HandleInput(SceneGame scene)
        {
            //NOOP
        }

        public virtual bool IsMouseOver(int x, int y)
        {
            return false;
        }

        public int GetStringHeight(string str, TextParameters parameters)
        {
            return FontUtil.GetStringHeight(FontUtil.FitString(FontUtil.StripFormat(str), parameters));
        }

        protected void DrawLabelledUI(SceneGame scene, SpriteReference sprite, Rectangle rectInterior, string label)
        {
            Rectangle rectExterior = new Rectangle(rectInterior.X, rectInterior.Y - 20, rectInterior.Width, 16);
            scene.DrawUI(sprite, rectInterior, Color.White);
            if (!string.IsNullOrWhiteSpace(label))
            {
                scene.DrawUI(sprite, rectExterior, Color.White);
                scene.DrawText(label, new Vector2(rectExterior.X, rectExterior.Y), Alignment.Center, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true).SetConstraints(rectExterior.Width - 16, rectExterior.Height));
            }
        }

        public abstract void Draw(SceneGame scene);
    }

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

        public override void Update(SceneGame scene)
        {
            if (Player.Dead && Player.CurrentAction.Done)
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
            else if (frontier.Any(front => front is IMineable))
            {
                TakeAction(Scheduler.Instance.RunAndWait(Player.RoutineAttack(dx, dy, Creature.MeleeAttack)), true);
            }
        }

        public void TakeAction(Wait wait, bool shouldBlock)
        {
            Player.CurrentAction = wait;
            if(shouldBlock)
                Scene.Wait.Add(Player.CurrentAction);
            Turn.End();
        }

        public override void HandleInput(SceneGame scene)
        {
            base.HandleInput(scene);

            InputTwinState state = Scene.InputState;

            if (GameOverMenu != null)
                GameOverMenu.HandleInput(scene);

            if (Player.Dead && Player.CurrentAction.Done)
            {
                if(GameOver.Done && GameOverMenu == null)
                {
                    GameOverMenu = new MenuTextSelection(string.Empty, new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height * 3 / 4), 300, 2);
                    GameOverMenu.Add(new ActAction("Restart", "Start over.", () =>
                    {
                        Scene.Restart();
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

            if (Player.CurrentAction.Done)
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
            }
            if (state.IsKeyPressed(Keys.Enter))
            {
                MenuTextSelection selection = new MenuTextSelection(String.Empty, new Vector2(Scene.Viewport.Width / 2, Scene.Viewport.Height * 3 / 4), 256, 8);

                Tile tile = Player.Tile;
                tile.AddActions(this, Player, selection);
                foreach (Tile neighbor in tile.GetAdjacentNeighbors())
                    neighbor.AddActions(this, Player, selection);
                selection.Add(new ActAction("Inventory", "Opens your inventory.", () =>
                {
                    selection.Close();
                    Open(new MenuInventory(this, Player));
                }));
                selection.AddDefault(new ActAction("Cancel", "Closes this menu.", () => selection.Close()));

                Open(selection);
            }
        }

        public override void Draw(SceneGame scene)
        {
            DrawSlot(scene, new Vector2(48, scene.Viewport.Height - 48), "Offhand", Player.EquipOffhand);
            DrawSlot(scene, new Vector2(96, scene.Viewport.Height - 64), "Body", Player.EquipBody);
            DrawSlot(scene, new Vector2(144, scene.Viewport.Height - 48), "Mainhand", Player.EquipMainhand);

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
            scene.DrawText(Game.ConvertToSmallPixelText(name), position + new Vector2(0, 20-8), Alignment.Center, new TextParameters().SetColor(Color.White, Color.Black));
            if (item != null)
            {
                scene.PushSpriteBatch(transform: Matrix.CreateScale(2, 2, 1));
                item.DrawIcon(scene, position / 2);
                scene.PopSpriteBatch();
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

        public override void HandleInput(SceneGame scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Escape))
                Close();
            base.HandleInput(scene);
        }

        public override void Select(int selection)
        {
            Item item = Items[selection];
            if(Filter(item))
            Parent.SelectItem(item);
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
            scene.DrawText($"{Game.FormatIcon(item)}{item.InventoryName}", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Filter(item) ? Color.White : Color.Gray, Color.Black));
        }
    }

    class MenuAnvil : Menu
    {
        class MenuCraftingSelection : MenuAct, IInventory
        {
            MenuAnvil MenuAnvil;
            Anvil Anvil => MenuAnvil.Anvil;
            SceneGame Scene => MenuAnvil.Scene;

            public override int SelectionCount => Parts.Length + 2;
            public Creature Holder => MenuAnvil.Holder;
           
            public Item[] Parts;
            public int CurrentPart;
            public PartType[] PartTypes;
            public Func<Material[], ToolCore> Create;
            public ToolCore Result;


            InventoryItemList ItemMenu;
            InfoBox InfoWindow;

            public MenuCraftingSelection(MenuAnvil menuAnvil, Vector2 position, string blueprintName, PartType[] parts) : base(blueprintName, position, 256, 8)
            {
                MenuAnvil = menuAnvil;
                PartTypes = parts;
                Parts = new Item[parts.Length];
                InfoWindow = new InfoBox(() => "Preview", () => GetResultDescription(), new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20 * 16);
                DefaultSelection = SelectionCount - 1;
            }

            private string GetResultDescription()
            {
                if(Result != null)
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
                }
            }

            public override void Update(SceneGame scene)
            {
                base.Update(scene);
                InfoWindow.Update(scene);
                if (ItemMenu != null)
                {
                    ItemMenu.Update(scene);
                }  
            }

            public override void HandleInput(SceneGame scene)
            {
                InfoWindow.HandleInput(scene);
                if (ItemMenu != null)
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

            public override void Draw(SceneGame scene)
            {
                base.Draw(scene);

                if(ItemMenu != null)
                    ItemMenu.Draw(scene);
               
                InfoWindow.Draw(scene);
            }

            public override void DrawLine(SceneGame scene, Vector2 linePos, int e)
            {
                SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
                if (Selection == e)
                    scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
                if(e < Parts.Length)
                {
                    IOre ore = Parts[e] as IOre;
                    Material material = ore?.Material;
                    string partName = $"{material?.Name ?? "No"} {PartTypes.GetName(e)}";
                    if(material != null)
                    {
                        var partSprite = PartTypes.GetSprite(e, material);
                        scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
                        {
                            scene.SetupColorMatrix(material.ColorTransform, matrix);
                        });
                        scene.DrawSprite(partSprite, 0, linePos + new Vector2(16, 0), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
                        scene.PopSpriteBatch();
                        
                    }
                    scene.DrawText(partName, linePos + new Vector2(32, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 48, 16).SetBold(true).SetColor(Color.White, Color.Black));
                }
                else if (e == Parts.Length)
                {
                    scene.DrawText("Build", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
                }
                else if (e == Parts.Length + 1)
                {
                    scene.DrawText("Cancel", linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
                }

            }

            public override void Select(int selection)
            {
                if (selection < Parts.Length)
                {
                    CurrentPart = selection;
                    ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height * 3 / 4), 256, 8) {
                        Filter = (item) => item is IOre ore && ore.CanUseInAnvil,
                    };
                }
                else if(selection == Parts.Length)
                {
                    Result.MoveTo(Anvil);
                    foreach (var part in Parts)
                        part.Destroy();
                    Close();
                }
                else if(selection == Parts.Length+1)
                {
                    foreach(Item item in Parts)
                    {
                        if(item != null)
                            Holder.Pickup(item);
                    }
                    Close();
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
                    if (partMaterial != null)
                    {
                        Parts[CurrentPart] = partMaterial;
                        Anvil.Container.Add(partMaterial, false);
                    }
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
                OpenBlueprintMenu("Blade", ToolBlade.Parts, (materials) => ToolBlade.Create(Scene, materials[0], materials[1], materials[2]));
            }));
            BlueprintMenu.Add(new ActAction("Adze", "Adzes can be used to mine blocks.", () =>
            {
                OpenBlueprintMenu("Adze", ToolAdze.Parts, (materials) => ToolAdze.Create(Scene, materials[0], materials[1], materials[2]));
            }));
            BlueprintMenu.Add(new ActAction("Plate", "Plates can be worn as armor.", () =>
            {
                OpenBlueprintMenu("Plate", ToolPlate.Parts, (materials) => ToolPlate.Create(Scene, materials[0], materials[1], materials[2]));
            }));
            BlueprintMenu.AddDefault(new ActAction("Cancel", "Closes this menu.", () =>
            {
                BlueprintMenu.Close();
            }));
        }

        private void OpenBlueprintMenu(string name, PartType[] parts, Func<Material[],ToolCore> create)
        {
            CraftingMenu = new MenuCraftingSelection(this, new Vector2(Scene.Viewport.Width * 1 / 4, Scene.Viewport.Height * 2 / 4), name, parts)
            {
                Create = create,
            };
        }

        public override void Update(SceneGame scene)
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

        public override void HandleInput(SceneGame scene)
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

        public override void Draw(SceneGame scene)
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

        public MenuSmelter(PlayerUI ui, Creature holder, Smelter smelter)
        {
            UI = ui;
            Holder = holder;
            Smelter = smelter;
            /*ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20)
            {
                Filter = (item) => item is IOre,
            };*/
        }

        public override bool IsMouseOver(int x, int y)
        {
            if (ItemMenu != null && ItemMenu.IsMouseOver(x, y))
                return true;
            if (ActionMenu != null && ActionMenu.IsMouseOver(x, y))
                return true;
            return base.IsMouseOver(x, y);
        }

        public override void Update(SceneGame scene)
        {
            if (ActionMenu != null)
            {
                ActionMenu.Update(scene);
            }
            if (ItemMenu != null)
            {
                ItemMenu.Update(scene);
            }
        }

        public override void HandleInput(SceneGame scene)
        {
            InputTwinState state = scene.InputState;

            if (ActionMenu != null)
            {
                ActionMenu.HandleInput(scene);
                if (ActionMenu.ShouldClose)
                    ActionMenu = null;
            }
            else if(ItemMenu != null)
            {
                ItemMenu.HandleInput(scene);
                if (ItemMenu.ShouldClose)
                    ItemMenu = null;
            }
            else
            {
                if (scene.InputState.IsKeyPressed(Keys.Escape))
                    Close();
                if (scene.InputState.IsKeyPressed(Keys.W, 15, 5))
                    Selection--;
                if (scene.InputState.IsKeyPressed(Keys.S, 15, 5))
                    Selection++;
                Selection = Clamp(Selection);
                if (scene.InputState.IsKeyPressed(Keys.Enter))
                {
                    switch (Selection)
                    {
                        case (SmelterSelection.Ore):
                            ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20)
                            {
                                Filter = (item) => item is IOre,
                            };
                            break;
                        case (SmelterSelection.Fuel):
                            ItemMenu = new InventoryItemList(this, new Vector2(Scene.Viewport.Width * 3 / 4, Scene.Viewport.Height / 2), 256, 20)
                            {
                                Filter = (item) => item is IFuel fuel && fuel.FuelTemperature > 0,
                            };
                            break;
                        case (SmelterSelection.Empty):
                            Smelter.Empty();
                            break;
                        case (SmelterSelection.Cancel):
                            Close();
                            break;
                    }
                }
            }
        }

        private SmelterSelection Clamp(SmelterSelection selection)
        {
            var selections = (SmelterSelection[])Enum.GetValues(typeof(SmelterSelection));
            return (SmelterSelection)(((int)selection + selections.Length) % selections.Length);
        }

        public override void Draw(SceneGame scene)
        {
            if (ItemMenu != null)
                ItemMenu.Draw(scene);
            if (ActionMenu != null)
                ActionMenu.Draw(scene);

            SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int widthSmelter = 256;
            int heightSmelter = 20 * 16;
            Rectangle rectSmelter = new Rectangle(Scene.Viewport.Width * 1 / 4 - widthSmelter / 2, Scene.Viewport.Height / 2 - heightSmelter / 2, widthSmelter, heightSmelter);
            DrawLabelledUI(scene, textbox, rectSmelter, $"{Smelter.Name}\n");

            TextParameters parameters = new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(rectSmelter);

            string description = String.Empty;
            Smelter.AddDescription(ref description);

            int currentY = 0;
            int remainingHeight = heightSmelter;
            scene.DrawText(description, new Vector2(rectSmelter.X, rectSmelter.Y + currentY), Alignment.Left, parameters);
            currentY += GetStringHeight(description, parameters);

            DrawLine(scene, SmelterSelection.Ore, new Vector2(rectSmelter.X, rectSmelter.Y + currentY), widthSmelter, "Add Ore");
            DrawLine(scene, SmelterSelection.Fuel, new Vector2(rectSmelter.X, rectSmelter.Y + currentY + 16), widthSmelter, "Add Fuel");
            DrawLine(scene, SmelterSelection.Empty, new Vector2(rectSmelter.X, rectSmelter.Y + currentY + 32), widthSmelter, "Empty");
            DrawLine(scene, SmelterSelection.Cancel, new Vector2(rectSmelter.X, rectSmelter.Y + currentY + 48), widthSmelter, "Cancel");
        }

        private void DrawLine(SceneGame scene, SmelterSelection selection, Vector2 linePos, int width, string name)
        {
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == selection)
                scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
            scene.DrawText(name, linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(width - 32, 16).SetBold(true).SetColor(Color.White, Color.Black));
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

        public override void Update(SceneGame scene)
        {
            base.Update(scene);

            ItemInfo.Update(scene);
            if (ItemActionMenu != null)
            {
                ItemActionMenu.Update(scene);
            }
            ItemMenu.Update(scene);
        }

        public override void HandleInput(SceneGame scene)
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

        public override void Draw(SceneGame scene)
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

        public override void HandleInput(SceneGame scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Enter) && WarnFrame.Done)
            {
                NextWarning();
            }
        }

        public override void Update(SceneGame scene)
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

        public override void Draw(SceneGame scene)
        {
            SpriteReference skull = SpriteLoader.Instance.AddSprite("content/ui_skull");
            SpriteReference text = SpriteLoader.Instance.AddSprite("content/ui_warntext");

            int middle = scene.Viewport.Height * 3 / 4;
            int size = (int)(80 * OpenFrame.Slide);
            double warnSlide = GetWarnSlide();
            int distExterior = (int)(60 * Math.Min(warnSlide, 0.5) * 2);
            int distInterior = (int)(60 * Math.Max(warnSlide - 0.5, 0) * 2);
            int distDelta = distExterior - distInterior;

            int textMiddle = (int)MathHelper.Lerp(0,size / 2 + text.Height / 2,OpenFrame.Slide);
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

    class InfoBox : Menu
    {
        public Func<string> Name;
        public Func<string> Text;

        public Vector2 Position;
        public int Width;
        public int Height;

        public InfoBox(Func<string> name, Func<string> text, Vector2 position, int width, int height)
        {
            Name = name;
            Text = text;
            Position = position;
            Width = width;
            Height = height;
        }

        public override bool IsMouseOver(int x, int y)
        {
            return new Rectangle((int)Position.X - Width / 2, (int)Position.Y - Height / 2, Width, Height).Contains(x, y);
        }

        public override void HandleInput(SceneGame scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Enter))
                Close();
            if (scene.InputState.IsKeyPressed(Keys.Escape))
                Close();
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
                DrawLabelledUI(scene, textbox, rect, openCoeff >= 1 ? Name() : string.Empty);
            if (openCoeff >= 1)
                scene.DrawText(Text(), new Vector2(x+8, y+4), Alignment.Left, new TextParameters().SetColor(Color.White,Color.Black).SetConstraints(Width - 16 - 16, Height-8));
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

        public override void Update(SceneGame scene)
        {
            base.Update(scene);
            if (Skill == null)
                ShouldClose = true;
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
                Scroll = Math.Max(Selection - ScrollHeight + 1, 0);
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
        public Func<bool> Enabled = () => true; 

        public ActAction(string name, string description, Action action, Func<bool> enabled = null)
        {
            Name = name;
            Action = action;
            if(enabled != null)
                Enabled = enabled;
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
            if(Actions[selection].Enabled())
                Actions[selection].Action();
        }

        public override void DrawLine(SceneGame scene, Vector2 linePos, int e)
        {
            ActAction action = Actions[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos, cursor.GetFrameRect(0), Color.White);
            Color color = Color.White;
            if (!action.Enabled())
                color = Color.Gray;
            scene.DrawText(action.Name, linePos + new Vector2(16, 0), Alignment.Left, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(color, Color.Black));
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
