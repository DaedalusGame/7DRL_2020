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
        bool IGameObject.Destroyed { get; set; }

        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public string EffectsString => string.Join(",\n", GetEffects<Effect>().Select(x => x.ToString()));
        public string StatString => string.Join(",\n", GetEffects<EffectStat>().GroupBy(stat => stat.Stat).Select(stat => $"{stat.Key} {stat.Sum(x => x.Amount)}"));
        public string EquipEffectsString => string.Join(",\n", GetEquipEffects(EquipSlot.Body).Select(x => x.ToString()));

        public int X => Tile.X;
        public int Y => Tile.Y;
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
        public Map Map => Tile?.Map;

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
            World.ToAdd.Enqueue(this);
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

        public virtual IEnumerable<Effect> GetEquipEffects(EquipSlot slot)
        {
            return Enumerable.Empty<Effect>();
        }

        public virtual IEnumerable<T> GetEffects<T>() where T : Effect
        {
            var list = new List<T>();
            list.AddRange(EffectManager.GetEffects<T>(this));
            return list;
        }

        public virtual void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            selection.Add(new ActAction($"Pick up {Game.FormatIcon(this)}{Name}", "Picks up the item and stores it in your inventory.", () =>
            {
                player.Pickup(this);
                selection.Close();
            }));
        }

        public virtual void AddItemActions(InventoryItemList inventory, Creature player, MenuTextSelection selection)
        {
            selection.Add(new ActAction("Throw Away", "Drop the item on the ground.", () =>
            {
                MoveTo(player.Tile);
                selection.Close();
                inventory.Reset();
            }));
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

        public bool ShouldDraw(Map map)
        {
            return Map == map;
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
        bool CanUseInAnvil
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
        double FuelTemperature
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
        public bool CanUseInAnvil => !Material.MeltingRequired;
        public double FuelTemperature => Material.FuelTemperature;

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

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Material.ColorTransform, matrix, projection);
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
        public bool CanUseInAnvil => true;
        public double FuelTemperature => Material.FuelTemperature;

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

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Material.ColorTransform, matrix, projection);
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

    enum PartShape
    {
        Head,
        Handle,
        Extra,
    }

    class PartType
    {
        public string Name;
        public string SpritePrefix;
        public double DurabilityMod;
        public PartShape Shape;

        public PartType(string name, string prefix, PartShape shape, double durabilityMod)
        {
            Name = name;
            SpritePrefix = prefix;
            DurabilityMod = durabilityMod;
            Shape = shape;
        }
    }

    abstract class ToolCore : Item
    {
        Material[] Materials;
        PartType[] Parts;
        protected abstract IEnumerable<EquipSlot> ValidSlots
        {
            get;
        }

        public double DurabilityMax => this.GetStat(Stat.Durability);
        public double Durability => DurabilityMax - this.GetTotalDamage();

        public ToolCore(SceneGame world, string name, string description, PartType[] parts) : base(world, name, description)
        {
            Materials = new Material[parts.Length];
            Parts = parts;
        }

        public override IEnumerable<T> GetEffects<T>()
        {
            var list = (List<T>)base.GetEffects<T>();
            for(int i = 0; i < Parts.Length; i++)
            {
                var part = GetMaterialPart(i);
                list.AddRange(part.GetItemEffects().SplitEffects<T>());
            }
            return list;
        }

        public override IEnumerable<Effect> GetEquipEffects()
        {
            List<Effect> effects = new List<Effect>();
            effects.AddRange(base.GetEquipEffects());
            for (int i = 0; i < Parts.Length; i++)
            {
                effects.AddRange(GetMaterialPart(i).GetEffects());
            }
            return effects;
        }

        public override IEnumerable<Effect> GetEquipEffects(EquipSlot slot)
        {
            List<Effect> effects = new List<Effect>();
            effects.AddRange(base.GetEquipEffects(slot));
            for(int i = 0; i < Parts.Length; i++)
            {
                effects.AddRange(GetMaterialPart(i).GetEffects(slot));
            }
            return effects;
        }

        public PartType GetPart(int part)
        {
            return Parts[part];
        }

        public Material.Part GetMaterialPart(int part)
        {
            return Materials[part].Parts[Parts[part]];
        }

        public string GetPartName(int part)
        {
            return Parts.GetName(part);
        }

        public SpriteReference GetPartSprite(int part, Material material)
        {
            return Parts.GetSprite(part, material);
        }

        public SpriteReference GetPartSprite(int part, Material material, string prefix)
        {
            return Parts.GetSprite(part, material, prefix);
        }

        public Material GetMaterial(int part)
        {
            return Materials[part];
        }

        public void SetMaterial(int part, Material material)
        {
            Materials[part] = material;
        }

        public override void AddItemActions(InventoryItemList inventory, Creature player, MenuTextSelection selection)
        {
            base.AddItemActions(inventory, player, selection);
            var currentEquip = GetEffects<EffectItemEquipped>();
            foreach (var slot in ValidSlots)
            {
                if(!currentEquip.Any(x => x.Slot == slot))
                selection.Add(new ActAction($"Equip ({slot})", "Equips the item to the specified slot.", () =>
                {
                    player.Equip(this, slot);
                    selection.Close();
                }));
            }
            if(currentEquip.Any())
                selection.Add(new ActAction($"Unequip", "Unequips the item.", () =>
                {
                    foreach(var equip in currentEquip)
                    {
                        equip.Remove();
                    }
                    selection.Close();
                }));
        }

        public override void AddStatBlock(ref string statBlock)
        {
            base.AddStatBlock(ref statBlock);
            //for(int i = 0; i < Materials.Length; i++)
            //    statBlock += $" {Game.FORMAT_BOLD}{GetPartName(i)}:{Game.FORMAT_BOLD} {GetMaterial(i).Name}\n";
            statBlock += "\n";
            statBlock += $"{Game.FormatStat(Stat.Durability)} Durability: {Durability}/{DurabilityMax}\n";
            var validSlots = ValidSlots;
            var generalBlock = GetEquipEffects();
            var blocks = validSlots.ToDictionary(slot => slot, slot => GetEquipEffects(slot).Where(effect => !generalBlock.Contains(effect)));

            AddStatBlock(ref statBlock, generalBlock);
            statBlock += $"\n";

            foreach (var block in blocks)
            {
                if (!block.Value.Any())
                    continue;
                statBlock += $"{Game.FORMAT_BOLD}In slot {block.Key}:{Game.FORMAT_BOLD}\n";
                AddStatBlock(ref statBlock, block.Value);
                statBlock += $"\n";
            }
        }

        private void AddStatBlock(ref string statBlock, IEnumerable<Effect> effects)
        {
            var effectGroups = effects.GroupBy(effect => effect, Effect.StatEquality);

            foreach (var group in effectGroups.OrderBy(group => group.Key.VisualPriority))
            {
                group.Key.AddStatBlock(ref statBlock, group);
            }
        }

        protected void PushMaterialBatch(SceneGame scene, Material material)
        {
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(material.ColorTransform, matrix, projection);
            });
        }

        protected void DrawSymbol(SceneGame scene, Vector2 position)
        {
            var equip = SpriteLoader.Instance.AddSprite("content/equip");

            if(GetEffects<EffectItemEquipped>().Any())
            {
                //scene.DrawSprite(equip, 0, position - equip.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            }
        }
    }

    class ToolBlade : ToolCore
    {
        public const int BLADE = 0;
        public const int GUARD = 1;
        public const int HANDLE = 2;

        public static PartType Blade = new PartType("Blade", "blade_", PartShape.Head, 1.0);
        public static PartType Guard = new PartType("Guard", "blade_", PartShape.Extra, 0.5);
        public static PartType Handle = new PartType("Handle", "blade_", PartShape.Handle, 0.5);

        public static PartType[] Parts = new[] { Blade, Guard, Handle };

        protected override IEnumerable<EquipSlot> ValidSlots => new[] { EquipSlot.Mainhand, EquipSlot.Offhand };

        public ToolBlade(SceneGame world) : base(world, "Blade", string.Empty, Parts)
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

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            Material bladeMaterial = GetMaterial(BLADE);
            Material guardMaterial = GetMaterial(GUARD);
            Material handleMaterial = GetMaterial(HANDLE);

            var blade = GetPartSprite(BLADE, bladeMaterial);
            var guard = GetPartSprite(GUARD, guardMaterial);
            var handle = GetPartSprite(HANDLE, handleMaterial);

            PushMaterialBatch(scene, guardMaterial);
            scene.DrawSprite(guard, 0, position - guard.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, handleMaterial);
            scene.DrawSprite(handle, 0, position - handle.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, bladeMaterial);
            scene.DrawSprite(blade, 0, position - blade.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();

            DrawSymbol(scene, position);
        }
    }

    class ToolAdze : ToolCore
    {
        public const int HEAD = 0;
        public const int BINDING = 1;
        public const int HANDLE = 2;

        public static PartType Head = new PartType("Head", "adze_", PartShape.Head, 1.0);
        public static PartType Binding = new PartType("Binding", "adze_", PartShape.Extra, 0.25);
        public static PartType Handle = new PartType("Handle", "adze_", PartShape.Handle, 0.5);

        public static PartType[] Parts = new[] { Head, Binding, Handle };

        protected override IEnumerable<EquipSlot> ValidSlots => new[] { EquipSlot.Mainhand, EquipSlot.Offhand };

        public ToolAdze(SceneGame world) : base(world, "Adze", string.Empty, Parts)
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

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            Material headMaterial = GetMaterial(HEAD);
            Material bindingMaterial = GetMaterial(BINDING);
            Material handleMaterial = GetMaterial(HANDLE);

            var head = GetPartSprite(HEAD, headMaterial);
            var binding = GetPartSprite(BINDING, bindingMaterial);
            var handle = GetPartSprite(HANDLE, handleMaterial);

            PushMaterialBatch(scene, bindingMaterial);
            scene.DrawSprite(binding, 0, position - binding.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, handleMaterial);
            scene.DrawSprite(handle, 0, position - handle.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, headMaterial);
            scene.DrawSprite(head, 0, position - head.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();

            DrawSymbol(scene, position);
        }
    }

    class ToolPlate : ToolCore
    {
        public const int CORE = 0;
        public const int COMPOSITE = 1;
        public const int TRIM = 2;

        public static PartType Core = new PartType("Core", "plate_", PartShape.Head, 3.0);
        public static PartType Composite = new PartType("Composite", "plate_", PartShape.Head, 1.0);
        public static PartType Trim = new PartType("Trim", "plate_", PartShape.Extra, 0.25);

        public static PartType[] Parts = new[] { Core, Composite, Trim };

        protected override IEnumerable<EquipSlot> ValidSlots => new[] { EquipSlot.Body, EquipSlot.Offhand, EquipSlot.Mainhand };

        public ToolPlate(SceneGame world) : base(world, "Plate", string.Empty, Parts)
        {

        }

        public static ToolPlate Create(SceneGame world, Material core, Material composite, Material trim)
        {
            ToolPlate tool = new ToolPlate(world);
            tool.SetMaterial(CORE, core);
            tool.SetMaterial(COMPOSITE, composite);
            tool.SetMaterial(TRIM, trim);
            return tool;
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            Material coreMaterial = GetMaterial(CORE);
            Material compositeMaterial = GetMaterial(COMPOSITE);
            Material trimMaterial = GetMaterial(TRIM);

            var core = GetPartSprite(CORE, coreMaterial);
            var composite = GetPartSprite(COMPOSITE, compositeMaterial);
            var trim = GetPartSprite(TRIM, trimMaterial);

            PushMaterialBatch(scene, coreMaterial);
            scene.DrawSprite(core, 0, position - core.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, compositeMaterial);
            scene.DrawSprite(composite, 0, position - composite.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, trimMaterial);
            scene.DrawSprite(trim, 0, position - trim.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();

            DrawSymbol(scene, position);
        }
    }

    class ToolArrow : ToolCore
    {
        public const int TIP = 0;
        public const int LIMB = 1;
        public const int FLETCHING = 2;

        public static PartType Tip = new PartType("Tip", "arrow_", PartShape.Head, 0.5);
        public static PartType Limb = new PartType("Limb", "arrow_", PartShape.Handle, 1.0);
        public static PartType Fletching = new PartType("Fletching", "arrow_", PartShape.Extra, 0.5);

        public static PartType[] Parts = new[] { Tip, Limb, Fletching };

        protected override IEnumerable<EquipSlot> ValidSlots => new[] { EquipSlot.Quiver };

        public ToolArrow(SceneGame world) : base(world, "Arrow", string.Empty, Parts)
        {

        }

        public static ToolArrow Create(SceneGame world, Material tip, Material limb, Material fletching)
        {
            ToolArrow arrow = new ToolArrow(world);
            arrow.SetMaterial(TIP, tip);
            arrow.SetMaterial(LIMB, limb);
            arrow.SetMaterial(FLETCHING, fletching);
            return arrow;
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            Material tipMaterial = GetMaterial(TIP);
            Material limbMaterial = GetMaterial(LIMB);
            Material fletchingMaterial = GetMaterial(FLETCHING);

            var tip = GetPartSprite(TIP, tipMaterial);
            var limb = GetPartSprite(LIMB, limbMaterial);
            var fletching = GetPartSprite(FLETCHING, fletchingMaterial);

            PushMaterialBatch(scene, limbMaterial);
            scene.DrawSprite(limb, 0, position - limb.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, tipMaterial);
            scene.DrawSprite(tip, 0, position - tip.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, fletchingMaterial);
            scene.DrawSprite(fletching, 0, position - fletching.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();

            DrawSymbol(scene, position);
        }

        public void DrawBullet(SceneGame scene, Vector2 position, float angle)
        {
            Material tipMaterial = GetMaterial(TIP);
            Material limbMaterial = GetMaterial(LIMB);
            Material fletchingMaterial = GetMaterial(FLETCHING);

            var tip = GetPartSprite(TIP, tipMaterial, "content/bullet_");
            var limb = GetPartSprite(LIMB, limbMaterial, "content/bullet_");
            var fletching = GetPartSprite(FLETCHING, fletchingMaterial, "content/bullet_");

            PushMaterialBatch(scene, limbMaterial);
            scene.DrawSpriteExt(limb, 0, position - limb.Middle, limb.Middle, angle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, tipMaterial);
            scene.DrawSpriteExt(tip, 0, position - tip.Middle, tip.Middle, angle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
            PushMaterialBatch(scene, fletchingMaterial);
            scene.DrawSpriteExt(fletching, 0, position - fletching.Middle, fletching.Middle, angle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        internal bool CanCollide(Creature user, Tile tile)
        {
            return Skills.Projectile.CollideSolid(user, tile);
        }

        public IEnumerable<Wait> Impact(Creature user, Tile tile)
        {
            Point velocity = user.Facing.ToOffset();
            List<Wait> waits = new List<Wait>();
            foreach (Creature creature in tile.Creatures)
            {
                user.Attack(creature, velocity.X, velocity.Y, ArrowAttack);
                waits.Add(creature.CurrentAction);
            }
            yield return new WaitAll(waits);
        }

        internal IEnumerable<Wait> Trail(Creature user, Tile tile)
        {
            return Skills.Projectile.NoTrail(user, tile);
        }

        public Attack ArrowAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(100, 0, 1);
            attack.ExtraEffects.Add(new AttackWeapon(this));
            attack.Elements.Add(Element.Pierce, 1.0);
            return attack;
        }
    }
}
