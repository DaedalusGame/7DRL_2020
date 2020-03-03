using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class Item : IEffectHolder, IGameObject
    {
        public SceneGame World { get; set; }
        public double DrawOrder => 0;
        bool IGameObject.Remove { get; set; }

        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public string EffectsString => string.Join(",\n", GetEffects<Effect>().Select(x => x.ToString()));
        public string StatString => string.Join(",\n", GetEffects<EffectStat>().GroupBy(stat => stat.Stat).Select(stat => $"{stat.Key} {stat.Sum(x => x.Amount)}"));
        public string EquipEffectsString => string.Join(",\n", GetEquipEffects().Select(x => x.ToString()));

        public Tile Tile
        {
            get
            {
                var tiles = EffectManager.GetEffects<OnTile.Primary>(this);
                if (tiles.Any())
                    return tiles.First().Tile;
                return null;
            }
        }

        public virtual string Name
        {
            get;
            set;
        }
        public virtual string InventoryName => Name;
        public string Description;

        List<Effect> EquipEffects = new List<Effect>();

        public Item(SceneGame world, string name, string description)
        {
            World = world;
            World.GameObjects.Add(this);
            ObjectID = EffectManager.NewID(this);
            Name = name;
            Description = description;
        }

        public void OnDestroy()
        {
            this.ClearEffects();
            EffectManager.DeleteHolder(this);
        }

        public virtual Item Split(int count)
        {
            return null;
        }

        public virtual bool Merge(Item item)
        {
            return false;
        }

        public void MoveTo(Tile tile)
        {
            tile.AddPrimary(this);
        }

        public void Update()
        {
            //NOOP
        }

        public void AddEquipEffect(Effect effect)
        {
            effect.Apply();
            EquipEffects.Add(effect);
        }

        public virtual IEnumerable<Effect> GetEquipEffects()
        {
            return EquipEffects.GetAndClean(effect => effect.Removed);
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            var list = new List<T>();
            list.AddRange(EffectManager.GetEffects<T>(this));
            return list;
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FormatIcon(this)}{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            if(!string.IsNullOrWhiteSpace(Description))
                tooltip += Description + "\n";
        }

        public virtual void AddStatBlock(ref string statBlock)
        {
            if(!string.IsNullOrWhiteSpace(Description))
                statBlock += $"{Description}\n";
        }

        public IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Item;
        }

        public void Draw(SceneGame scene, DrawPass pass)
        {
            Tile tile = Tile;
            if(tile != null)
                DrawIcon(scene, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8));
        }

        public abstract void DrawIcon(SceneGame scene, Vector2 position);

        public override string ToString()
        {
            return $"Item {ObjectID.ID}";
        }
    }

    interface IOre
    {
        Material Material
        {
            get;
        }
        int Amount
        {
            get;
        }
        int Reduce(int amount);
    }

    interface IFuel
    {
        Material Material
        {
            get;
        }
        double Temperature
        {
            get;
        }
        int Amount
        {
            get;
        }
    }

    class Ore : Item, IOre, IFuel
    {
        public override string Name { get => $"{Material.Name} Ore"; set {} }
        public override string InventoryName => $"{Name} [{Amount}]";

        public Material Material
        {
            get;
            set;
        }
        public int Amount
        {
            get;
            set;
        }
        public double Temperature => Material.FuelTemperature;

        public Ore(SceneGame world, Material material, int amount) : base(world, "Ore", string.Empty)
        {
            Material = material;
            Amount = amount;
        }

        public override void AddStatBlock(ref string statBlock)
        {
            statBlock += $"{Amount} Pieces";
            base.AddStatBlock(ref statBlock);
        }

        public override bool Merge(Item item)
        {
            if(item != this && item is Ore ore && ore.Material == Material)
            {
                Amount += ore.Amount;
                return true;
            }
            return false;
        }

        public override Item Split(int count)
        {
            count = Math.Min(count, Amount);
            Reduce(count);
            if (Amount <= 0)
                this.Destroy();
            return new Ore(World, Material, count);
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            var ore = SpriteLoader.Instance.AddSprite("content/item_ore");

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Material.ColorTransform, matrix);
            });
            scene.DrawSprite(ore, 0, position - ore.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        public int Reduce(int amount)
        {
            Amount -= amount;
            return Amount;
        }
    }

    class Ingot : Item, IOre, IFuel
    {
        public override string Name { get => $"{Material.Name} Ingot"; set { } }
        public override string InventoryName => $"{Name} [{Count}]";

        public Material Material
        {
            get;
            set;
        }
        public int Count;
        public int Amount => Count * 200;
        public double Temperature => Material.FuelTemperature;

        public Ingot(SceneGame world, Material material, int count) : base(world, "Ingot", string.Empty)
        {
            Material = material;
            Count = count;
        }

        public override bool Merge(Item item)
        {
            if (item != this && item is Ingot ingot && ingot.Material == Material)
            {
                Count += ingot.Count;
                return true;
            }
            return false;
        }

        public override Item Split(int count)
        {
            count = Math.Min(count, Count);
            Count -= count;
            if (Amount <= 0)
                this.Destroy();
            return new Ingot(World, Material, count);
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            var ingot = SpriteLoader.Instance.AddSprite("content/item_ingot");

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Material.ColorTransform, matrix);
            });
            scene.DrawSprite(ingot, 0, position - ingot.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        public int Reduce(int amount)
        {
            Count -= Amount / amount;
            return Amount;
        }
    }

    abstract class ToolCore : Item
    {
        Material[] Materials;

        public ToolCore(SceneGame world, string name, string description, int parts) : base(world, name, description)
        {
            Materials = new Material[parts];
        }

        public abstract string GetPartName(int part);

        public Material GetMaterial(int part)
        {
            return Materials[part];
        }

        public void SetMaterial(int part, Material material)
        {
            Materials[part] = material;
        }

        public override void AddStatBlock(ref string statBlock)
        {
            base.AddStatBlock(ref statBlock);
            for(int i = 0; i < Materials.Length; i++)
                statBlock += $"{Game.FORMAT_BOLD}{GetPartName(i)}:{Game.FORMAT_BOLD} {GetMaterial(i).Name}\n";
        }

        protected void PushMaterialBatch(SceneGame scene, Material material)
        {
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(material.ColorTransform, matrix);
            });
        }
    }

    class ToolBlade : ToolCore
    {
        public const int BLADE = 0;
        public const int GUARD = 1;
        public const int HANDLE = 2;

        public ToolBlade(SceneGame world) : base(world, "Blade",string.Empty,3)
        {
            
        }

        public static ToolBlade Create(SceneGame world, Material blade, Material guard, Material handle)
        {
            ToolBlade tool = new ToolBlade(world);
            tool.SetMaterial(BLADE, blade);
            tool.SetMaterial(GUARD, guard);
            tool.SetMaterial(HANDLE, handle);
            return tool;
        }

        public override string GetPartName(int part)
        {
            switch(part)
            {
                case (BLADE):
                    return "Blade";
                case (GUARD):
                    return "Guard";
                case (HANDLE):
                    return "Handle";
                default:
                    return string.Empty;
            }
        }

        public override IEnumerable<Effect> GetEquipEffects()
        {
            List<Effect> effects = new List<Effect>();
            effects.AddRange(base.GetEquipEffects());
            effects.AddRange(GetMaterial(BLADE).BladeBlade.GetEffects());
            effects.AddRange(GetMaterial(GUARD).BladeGuard.GetEffects());
            effects.AddRange(GetMaterial(HANDLE).BladeHandle.GetEffects());
            return effects;
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            Material bladeMaterial = GetMaterial(BLADE);
            Material guardMaterial = GetMaterial(GUARD);
            Material handleMaterial = GetMaterial(HANDLE);

            var blade = SpriteLoader.Instance.AddSprite("content/blade_" + bladeMaterial.BladeBlade.Sprite);
            var guard = SpriteLoader.Instance.AddSprite("content/blade_" + guardMaterial.BladeGuard.Sprite);
            var handle = SpriteLoader.Instance.AddSprite("content/blade_" + handleMaterial.BladeHandle.Sprite);

            PushMaterialBatch(scene, guardMaterial);
            scene.DrawSprite(guard, 0, position - guard.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, handleMaterial);
            scene.DrawSprite(handle, 0, position - handle.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, bladeMaterial);
            scene.DrawSprite(blade, 0, position - blade.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }
    }

    class ToolAdze : ToolCore
    {
        public const int HEAD = 0;
        public const int BINDING = 1;
        public const int HANDLE = 2;

        public ToolAdze(SceneGame world) : base(world, "Adze", string.Empty, 3)
        {

        }

        public static ToolAdze Create(SceneGame world, Material head, Material binding, Material handle)
        {
            ToolAdze tool = new ToolAdze(world);
            tool.SetMaterial(HEAD, head);
            tool.SetMaterial(BINDING, binding);
            tool.SetMaterial(HANDLE, handle);
            return tool;
        }

        public override string GetPartName(int part)
        {
            switch (part)
            {
                case (HEAD):
                    return "Head";
                case (BINDING):
                    return "Binding";
                case (HANDLE):
                    return "Handle";
                default:
                    return string.Empty;
            }
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            throw new NotImplementedException();
        }
    }
}
