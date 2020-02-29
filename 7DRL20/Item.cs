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
    abstract class Item : IEffectHolder
    {
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

        public string Name;
        public string Description;

        List<Effect> EquipEffects = new List<Effect>();

        public Item(string name, string description)
        {
            ObjectID = EffectManager.NewID();
            Name = name;
            Description = description;
        }

        public void MoveTo(Tile tile)
        {
            tile.AddPrimary(this);
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
            tooltip += Game.FORMAT_BOLD + Name + Game.FORMAT_BOLD + "\n";
            tooltip += Description + "\n";
        }

        public abstract void DrawIcon(SceneGame scene, Vector2 position);

        public override string ToString()
        {
            return $"Item {ObjectID.ID}";
        }
    }

    class Ore : Item
    {
        Material Material;
        int Amount;

        public Ore() : base("Ore", string.Empty)
        {
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            throw new NotImplementedException();
        }
    }

    class Ingot : Item
    {
        Material Material;

        public Ingot() : base("Ingot", string.Empty)
        {
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            throw new NotImplementedException();
        }
    }

    abstract class ToolCore : Item
    {
        Material[] Materials;

        public ToolCore(string name, string description, int parts) : base(name, description)
        {
            Materials = new Material[parts];
        }

        public Material GetMaterial(int part)
        {
            return Materials[part];
        }

        public void SetMaterial(int part, Material material)
        {
            Materials[part] = material;
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

        public ToolBlade() : base("Blade",string.Empty,3)
        {
            
        }

        public static ToolBlade Create(Material blade, Material guard, Material handle)
        {
            ToolBlade tool = new ToolBlade();
            tool.SetMaterial(BLADE, blade);
            tool.SetMaterial(GUARD, guard);
            tool.SetMaterial(HANDLE, handle);
            return tool;
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

        public ToolAdze() : base("Adze", string.Empty, 3)
        {

        }

        public static ToolAdze Create(Material head, Material binding, Material handle)
        {
            ToolAdze tool = new ToolAdze();
            tool.SetMaterial(HEAD, head);
            tool.SetMaterial(BINDING, binding);
            tool.SetMaterial(HANDLE, handle);
            return tool;
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            throw new NotImplementedException();
        }
    }
}
