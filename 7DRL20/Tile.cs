using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;

namespace RoguelikeEngine
{
    interface IMineable
    {
        void Mine(MineEvent mine);

        void Destroy();
    }

    abstract class Tile : IEffectHolder, IHasPosition
    {
        public static TileColor HiddenColor = new TileColor(Color.Black, Color.Black);

        protected MapTile Parent;
        public ReusableID ObjectID
        {
            get;
            set;
        }
        public SceneGame World => Parent.World;
        public Map Map => Parent.Map;
        public int X => Parent.X;
        public int Y => Parent.Y;
        public Vector2 VisualPosition => new Vector2(X*16,Y*16);
        public Vector2 VisualTarget => VisualPosition + new Vector2(8, 8);
        public Tile Under => Parent.UnderTile;
        public bool Orphaned => false;

        public Func<Color> VisualUnderColor = () => Color.TransparentBlack;

        public string Name;

        public bool Opaque;
        public bool Solid;

        public GeneratorGroup Group
        {
            get
            {
                return Parent.Group;
            }
            set
            {
                Parent.Group = value;
            }
        }

        public Tile NewTile => Parent.Tile;
        public IEnumerable<IEffectHolder> Contents => Parent.GetEffects<Effects.OnTile>().Select(x => x.Holder);
        public IEnumerable<Creature> Creatures => Contents.OfType<Creature>();
        public IEnumerable<Item> Items => Contents.OfType<Item>();

        List<Effect> TileEffects = new List<Effect>();
        
        public Tile(string name)
        {
            ObjectID = EffectManager.NewID(this);
            Name = name;
        }

        public bool IsVisible()
        {
            return !Opaque || (Map.InBounds(X, Y) && GetAllNeighbors().Where(p => Map.InMap(p.X, p.Y)).Any(neighbor => !neighbor.Opaque));
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public void AddTileEffect(Effect effect)
        {
            effect.Apply();
            TileEffects.Add(effect);
        }

        public IEnumerable<Effect> GetTileEffects()
        {
            return TileEffects.GetAndClean(effect => effect.Removed);
        }

        public void Replace(Tile newTile)
        {
            Parent.Set(newTile);
        }

        public void PlaceOn(Tile newTile)
        {
            Parent.SaveUnder();
            Parent.Set(newTile);
        }

        public void Scrape()
        {
            Parent.RestoreUnder();
        }

        public void MakeFloor()
        {
            if (Parent.UnderTile != null && !Parent.UnderTile.Solid)
                Scrape();
            else
                Replace(new FloorCave());
        }

        public void SetParent(MapTile parent)
        {
            Parent = parent;
        }

        public Tile GetNeighbor(int dx, int dy)
        {
            return Map.GetTile(X + dx, Y + dy);
        }

        public IEnumerable<Tile> GetAdjacentNeighbors()
        {
            return new[] { GetNeighbor(1, 0), GetNeighbor(0, 1), GetNeighbor(-1, 0), GetNeighbor(0, -1) };
        }

        public IEnumerable<Tile> GetAllNeighbors()
        {
            return new[] { GetNeighbor(1, 0), GetNeighbor(0, 1), GetNeighbor(-1, 0), GetNeighbor(0, -1), GetNeighbor(1, 1), GetNeighbor(-1, 1), GetNeighbor(-1, -1), GetNeighbor(1, -1) };
        }

        public IEnumerable<Tile> GetNearby(int radius)
        {
            return Map.GetNearby(X, Y, radius);
        }

        public IEnumerable<Tile> GetNearby(Rectangle rectangle, int radius)
        {
            return Map.GetNearby(rectangle, radius);
        }

        public void AddPrimary(IEffectHolder holder)
        {
            Effect.Apply(new Effects.OnTile.Primary(Parent, holder));
        }

        public void Add(IEffectHolder holder)
        {
            Effect.Apply(new Effects.OnTile(Parent, holder));
        }

        protected Color GetUnderColor(SceneGame scene)
        {
            Color glow = Group.GlowColor(scene.Frame);
            Color underColor = VisualUnderColor();
            if (IsVisible())
                return new Color(glow.R + underColor.R, glow.G + underColor.G, glow.B + underColor.B, glow.A + underColor.A);
            else
                return underColor;
        }

        public virtual void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            foreach (Item item in Items)
            {
                item.AddActions(ui, player, selection);
            }
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            if(Creatures.Any())
                tooltip += "\n";
            int creatureCount = 0;
            foreach (Creature creature in Creatures.Take(10+1))
            {
                if (creatureCount >= 10)
                    tooltip += "...\n";
                else
                    creature.AddTooltip(ref tooltip);
                creatureCount++;
            }
            if (Items.Any())
                tooltip += "\n";
            int itemCount = 0;
            foreach (Item item in Items.Take(10+1))
            {
                if (itemCount >= 10)
                    tooltip += "...\n";
                else
                    item.AddTooltip(ref tooltip);
                itemCount++;
            }
        }

        public abstract void Draw(SceneGame scene);
    }

    struct TileColor
    {
        static ColorMatrix FloorBackground = ColorMatrix.Saturate(0.5f) * ColorMatrix.Scale(0.25f);
        static ColorMatrix FloorForeground = ColorMatrix.Saturate(0.5f) * ColorMatrix.Scale(0.35f);

        public Color Background;
        public Color Foreground;

        public TileColor(Color background, Color foreground)
        {
            Background = background;
            Foreground = foreground;
        }

        public TileColor ToFloor()
        {
            return new TileColor(FloorBackground.Transform(Background), FloorForeground.Transform(Foreground));
        }
    }

    class FloorCave : Tile
    {
        public FloorCave() : base("Cave Floor")
        {
        }

        public override void Draw(SceneGame scene)
        {
            var floor = SpriteLoader.Instance.AddSprite("content/tile_floor");
            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            var color = Group.CaveColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }
    }

    class FloorTiles : Tile
    {
        public FloorTiles() : base("Tiled Floor")
        {
        }

        public override void Draw(SceneGame scene)
        {
            var floor = SpriteLoader.Instance.AddSprite("content/tile_floor");
            var cave0 = SpriteLoader.Instance.AddSprite("content/dancefloor_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/dancefloor_layer");

            var color = Group.CaveColor.ToFloor();
            Color glow = Group.GlowColor(scene.Frame);

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }
    }

    class WallCave : Tile, IMineable
    {
        public WallCave() : base("Cave Wall")
        {
            Solid = true;
            Opaque = true;
        }

        public override void Draw(SceneGame scene)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            var color = Group.CaveColor;
            Color glow = Group.GlowColor(scene.Frame);

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }

        public void Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.25, LootGenerator);
            mine.Start();
        }

        private void LootGenerator(Creature creature)
        {
            //NOOP
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    class WallOre : Tile, IMineable
    {
        static Random Random = new Random();
        int Frame = Random.Next(1000);
        Material Material;

        public WallOre(Material material) : base($"{material.Name} Vein")
        {
            Material = material;
            Solid = true;
            Opaque = true;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene)
        {
            if (Under != null)
                Under.Draw(scene);
            if (!IsVisible())
                return;
            var ore = SpriteLoader.Instance.AddSprite("content/ore");

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Material.ColorTransform, matrix);
            });
            scene.DrawSprite(ore, Frame, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);
            scene.PopSpriteBatch();
        }

        public void Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.1, LootGenerator);
            mine.Start();
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Scrape();
        }
    }

    class WallObsidiorite : Tile, IMineable
    {
        public WallObsidiorite() : base("Obsidiorite")
        {
            Solid = true;
            Opaque = true;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene)
        {
            var color = new TileColor(new Color(69 / 2, 54 / 2, 75 / 2), new Color(157, 143, 167));
            Color glow = Color.TransparentBlack;

            if (!IsVisible())
            {
                color = HiddenColor;
                glow = Color.TransparentBlack;
            }

            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            glow = Util.AddColor(glow, VisualUnderColor());
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), glow);
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }

        public void Mine(MineEvent mine)
        {
            mine.Setup(this, 2, 0.02, LootGenerator);
            mine.Start();
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material.Obsidiorite, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    class WallMeteorite : Tile, IMineable
    {
        public WallMeteorite() : base("Meteorite")
        {
            Solid = true;
            Opaque = true;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene)
        {
            var color = new TileColor(new Color(69, 75, 54), new Color(157, 167, 143));
            Color glow = Color.Lerp(new Color(16, 4, 1), new Color(255, 64, 16), 0.5f + 0.5f * (float)Math.Sin(scene.Frame / 60f));

            if (!IsVisible())
            {
                color = HiddenColor;
                glow = Color.TransparentBlack;
            }

            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            glow = Util.AddColor(glow, VisualUnderColor());
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), glow);
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }

        public void Mine(MineEvent mine)
        {
            mine.Setup(this, 2, 0.01, LootGenerator);
            mine.Start();
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material.Meteorite, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    class WallBasalt : Tile, IMineable
    {
        public WallBasalt() : base("Basalt")
        {
            Solid = true;
            Opaque = true;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene)
        {
            var color = new TileColor(new Color(169, 169, 169), new Color(239, 236, 233));
            Color glow = new Color(128, 128, 128);

            if (!IsVisible())
            {
                color = HiddenColor;
                glow = Color.TransparentBlack;
            }

            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            glow = Util.AddColor(glow, VisualUnderColor());
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), glow);
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }

        public void Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.1, LootGenerator);
            mine.Start();
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material.Basalt, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    class WallBrick : Tile, IMineable
    {
        public WallBrick() : base("Brick Wall")
        {
            Solid = true;
            Opaque = true;
        }

        public override void Draw(SceneGame scene)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/brick_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/brick_layer");

            var color = Group.BrickColor;

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), VisualUnderColor());
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }

        public void Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.1, LootGenerator);
            mine.Start();
        }

        private void LootGenerator(Creature creature)
        {
            //NOOP
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    class Anvil : Tile
    {
        public Container Container;

        public Anvil() : base("Anvil")
        {
            Solid = true;

            Container = new Container();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            base.AddActions(ui, player, selection);
            selection.Add(new ActAction("Anvil", () =>
            {
                selection.Close();
                ui.Open(new MenuAnvil(ui, player, this));
            }));
        }

        public override void Draw(SceneGame scene)
        {
            var anvil = SpriteLoader.Instance.AddSprite("content/anvil");

            if (Under != null)
                Under.Draw(scene);
            if (!IsVisible())
                return;

            scene.DrawSprite(anvil, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);
        }

        public void Empty()
        {
            foreach (Item item in Container.Items)
                item.MoveTo(this);
        }
    }

    class Smelter : Tile, ITurnTaker
    {
        public double TurnSpeed => 1.0f;
        public double TurnBuildup { get; set; }
        public bool TurnReady => TurnBuildup >= 1;
        public bool RemoveFromQueue => Orphaned;

        public double Ready;
        public Container OreContainer;
        public Container FuelContainer;
        public Dictionary<Material, int> Fuels = new Dictionary<Material, int>();

        public double FuelTemperature => Fuels.Any() ? Fuels.Max(x => x.Key.FuelTemperature) : 0;
        public int FuelAmount => Fuels.Sum(fuel => fuel.Value);
        public double SpeedBoost => 1.0f;

        public Smelter(SceneGame world) : base("Smelter")
        {
            Solid = true;

            world.ActionQueue.Add(this);
            OreContainer = new Container();
            FuelContainer = new Container();
        }

        private void Work()
        {
            ConsumeFuel();

            if (!HasValidWork() || !HasFuel())
            {
                Ready = 0;
                return;
            }

            if (Ready >= 1)
            {
                IEnumerable<Item> ores = OreContainer.Items.Where(item => item is IOre);
                Dictionary<Material, int> alloySoup = ores.OfType<IOre>().GroupBy(ore => ore.Material).ToDictionary(group => group.Key, group => group.Sum(ore => ore.Amount));

                foreach(Material alloy in Material.Alloys.OrderBy(alloy => alloy.Priority))
                {
                    alloy.MakeAlloy(alloySoup);
                }

                foreach(Item item in ores)
                {
                    item.Destroy();
                }

                foreach(var pair in alloySoup)
                {
                    int value = pair.Value;
                    if(value >= 200)
                    {
                        int ingots = value / 200;
                        Ingot ingot = new Ingot(World, pair.Key, ingots);
                        ingot.MoveTo(this);
                        value -= ingots * 200;
                    }
                    if (value > 0)
                    {
                        Ore leftovers = new Ore(World, pair.Key, value);
                        OreContainer.Add(leftovers, true);
                    }
                }
                
                Ready = 0;
            }
            else
            {
                Ready += 0.2f * SpeedBoost;
                Ready = Math.Max(Math.Min(Ready, 1), 0);
            }
        }

        private void ConsumeFuel()
        {
            RemoveFuel(1);

            foreach (var item in FuelContainer.Items)
            {
                if (item is IFuel fuel)
                {
                    if (!Fuels.ContainsKey(fuel.Material))
                    {
                        Fuels.Add(fuel.Material, fuel.Amount);
                        item.Destroy();
                    }
                }
            }
        }

        private void RemoveFuel(int i)
        {
            foreach (var key in Fuels.Keys.ToList())
            {
                Fuels[key] -= i;
                if (Fuels[key] <= 0)
                    Fuels.Remove(key);
            }
        }

        private bool HasValidWork()
        {
            return OreContainer.Items.Any(x => x is IOre ore && ore.Material.MeltingTemperature <= FuelTemperature);
        }

        private bool HasFuel()
        {
            return Fuels.Max(x => x.Value) > OreContainer.Items.OfType<IOre>().Where(x => x.Material.MeltingTemperature <= FuelTemperature).Sum(x => x.Amount) / 50;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            AddDescription(ref tooltip);
            base.AddTooltip(ref tooltip);
        }

        public void AddDescription(ref string tooltip)
        {
            tooltip += $"Ready: {(int)(Math.Round(Ready, 2) * 100)}% ({Math.Round(SpeedBoost, 1)}x Speed)\n";
            tooltip += $"Heat: {FuelTemperature} ({FuelAmount} Fuel left)\n";
            tooltip += "\n";
            tooltip += "Ore:\n";
            if (OreContainer.Items.Any())
                foreach (var item in OreContainer.Items)
                    tooltip += $"- {Game.FormatIcon(item)}{Game.FORMAT_BOLD}{item.InventoryName}{Game.FORMAT_BOLD}\n";
            else
                tooltip += "- Empty\n";
            tooltip += "Fuel:\n";
            if (FuelContainer.Items.Any())
                foreach (var item in FuelContainer.Items)
                    tooltip += $"- {Game.FormatIcon(item)}{Game.FORMAT_BOLD}{item.InventoryName}{Game.FORMAT_BOLD}\n";
            else
                tooltip += "- Empty\n";
        }

        public override void Draw(SceneGame scene)
        {
            var smelter = SpriteLoader.Instance.AddSprite("content/smelter_receptacle");
            var smelter_overlay = SpriteLoader.Instance.AddSprite("content/smelter_receptacle_overlay");

            if (!IsVisible())
                return;

            scene.DrawSprite(smelter, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);

            var allMaterials = new Dictionary<Material, float>();

            foreach (var ore in OreContainer.Items.OfType<IOre>())
                if (allMaterials.ContainsKey(ore.Material))
                    allMaterials[ore.Material] += ore.Amount;
                else
                    allMaterials.Add(ore.Material, ore.Amount);
            foreach (var fuel in FuelContainer.Items.OfType<IFuel>())
                if (allMaterials.ContainsKey(fuel.Material))
                    allMaterials[fuel.Material] += fuel.Amount;
                else
                    allMaterials.Add(fuel.Material, fuel.Amount);
            foreach(var fuel in Fuels)
                if (allMaterials.ContainsKey(fuel.Key))
                    allMaterials[fuel.Key] += fuel.Value;
                else
                    allMaterials.Add(fuel.Key, fuel.Value);

            if (allMaterials.Any())
            {
                ColorMatrix color = ColorMatrix.Lerp(allMaterials.ToDictionary(x => x.Key.ColorTransform, x => x.Value));

                scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
                {
                    scene.SetupColorMatrix(color, matrix);
                });
                scene.DrawLava(new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16));
                scene.PopSpriteBatch();
            }
            
            scene.DrawSprite(smelter_overlay, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);
        }

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            base.AddActions(ui, player, selection);
            selection.Add(new ActAction("Smelter", () =>
            {
                selection.Close();
                ui.Open(new MenuSmelter(ui, player, this));
            }));
        }

        Wait ITurnTaker.TakeTurn(ActionQueue queue)
        {
            Work();

            this.ResetTurn();
            return Wait.NoWait;
        }

        public void Empty()
        {
            foreach (Item item in OreContainer.Items)
                item.MoveTo(this);
            foreach (Item item in FuelContainer.Items)
                item.MoveTo(this);
        }
    }
}
