using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Menus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    [SerializeInfo]
    abstract class Item : IEffectHolder, IGameObject, IJsonSerializable
    {
        public SceneGame World { get; set; }
        public double DrawOrder => 0;
        public bool Destroyed { get; set; }

        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public Guid GlobalID
        {
            get;
            private set;
        }
        //public string EffectsString => string.Join(",\n", GetEffects<Effect>().Select(x => x.ToString()));
        //public string StatString => string.Join(",\n", GetEffects<EffectStat>().GroupBy(stat => stat.Stat).Select(stat => $"{stat.Key} {stat.Sum(x => x.Amount)}"));
        //public string EquipEffectsString => string.Join(",\n", GetEquipEffects(EquipSlot.Body).Select(x => x.ToString()));

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
        public IEffectHolder Owner
        {
            get
            {
                var owners = EffectManager.GetEffects<EffectItemInventory>(this);
                if (owners.Any())
                    return owners.First().Holder;
                return null;
            }
        }
        public Map Map
        {
            get
            {
                if (Tile != null)
                    return Tile.Map;
                else if (Owner is IJsonSerializable serializable)
                    return serializable.Map;
                else
                    return null;
            }
            set
            {
                //NOOP
            }
        }

        public virtual string BaseName
        {
            get;
            set;
        }
        public virtual string Name => this.GetName(BaseName);
        public virtual string InventoryName => BaseName;

        public string Description;

        public bool CanPickup = true;

        List<Effect> EquipEffects = new List<Effect>();

        public Item(SceneGame world, string name, string description)
        {
            World = world;
            World.ToAdd.Enqueue(this);
            ObjectID = EffectManager.SetID(this);
            GlobalID = EffectManager.SetGlobalID(this);
            BaseName = name;
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

        public void AddNormalTurn()
        {
            ActionQueue queue = World.ActionQueue;
            queue.Add(new TurnTakerItem(queue, this));
        }

        public virtual Wait NormalTurn(Turn turn)
        {
            return Wait.NoWait;
        }

        public virtual void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            if(CanPickup)
            selection.Add(new ActAction($"Pick up {Game.FormatIcon(this)}{BaseName}", "Picks up the item and stores it in your inventory.", () =>
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

        public virtual IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Item;
        }

        public virtual void Draw(SceneGame scene, DrawPass pass)
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

        public virtual JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["objectId"] = Serializer.GetHolderID(this);
            return json;
        }

        public virtual void ReadJson(JToken json, Context context)
        {
            Guid globalId = Guid.Parse(json["objectId"].Value<string>());
            GlobalID = EffectManager.SetGlobalID(this, globalId);
        }

        public void AfterLoad()
        {
            //NOOP
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

        bool CanUseInAnvil(PartType partType);

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
        public override string BaseName { get => $"{Material.Name} Ore"; set {} }
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
        public double FuelTemperature => Material.FuelTemperature;

        public Ore(SceneGame world) : base(world, "Ore", string.Empty)
        {

        }

        public Ore(SceneGame world, Material material, int amount) : this(world)
        {
            Material = material;
            Amount = amount;
        }

        [Construct("ore")]
        public static Ore Construct(Context context)
        {
            return new Ore(context.World);
        }

        public bool CanUseInAnvil(PartType partType)
        {
            return !Material.MeltingRequired && Material.IsPartValid(partType);
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

        public override JToken WriteJson(Context context)
        {
            JToken json = base.WriteJson(context);
            json["material"] = Material.ID;
            json["amount"] = Amount;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Material = Material.GetMaterial(json["material"].Value<string>());
            Amount = json["amount"].Value<int>();
        }
    }

    abstract class OreItem : Item, IOre
    {
        public override string InventoryName => $"{Name} [{Count}]";

        public Material Material
        {
            get;
            set;
        }
        public int Amount
        {
            get
            {
                return Count * AmountPerItem;
            }
            set
            {
                //NOOP
            }
        }

        public int Count;
        public int AmountPerItem = 0;

        protected OreItem(SceneGame world, string name) : base(world, name, string.Empty)
        {
        }

        public virtual bool CanUseInAnvil(PartType partType)
        {
            return Material.IsPartValid(partType);
        }

        public override bool Merge(Item item)
        {
            if (item != this && item is OreItem ore && item.GetType() == GetType())
            {
                Count += ore.Count;
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
            return CopyWithCount(count);
        }

        public abstract Item CopyWithCount(int count);

        public int Reduce(int amount)
        {
            Count -= Amount / amount;
            return Amount;
        }

        public override JToken WriteJson(Context context)
        {
            JToken json = base.WriteJson(context);
            json["count"] = Count;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Count = json["count"].Value<int>();
        }
    }

    class ItemHandle : OreItem
    {
        static HashSet<PartType> ValidParts = new HashSet<PartType>()
        {
            ToolBlade.Guard,
            ToolBlade.Handle,
            ToolAdze.Binding,
            ToolAdze.Handle,
            ToolArrow.Limb,
        };

        public ItemHandle(SceneGame world) : base(world, "Wooden Handle")
        {
            Material = Material.Wood;
            AmountPerItem = 200;
        }

        public ItemHandle(SceneGame world, int count) : this(world)
        {
            Count = count;
        }

        [Construct("handle_wood")]
        public static ItemHandle Construct(Context context)
        {
            return new ItemHandle(context.World);
        }

        public override bool CanUseInAnvil(PartType partType)
        {
            return base.CanUseInAnvil(partType) && ValidParts.Contains(partType);
        }

        public override Item CopyWithCount(int count)
        {
            return new ItemHandle(World, count);
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            var ingot = SpriteLoader.Instance.AddSprite("content/item_handle");
            scene.DrawSprite(ingot, 0, position - ingot.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
    }

    class ItemFeather : OreItem
    {
        public ItemFeather(SceneGame world) : base(world, "Feather")
        {
            Material = Material.Feather;
            AmountPerItem = 200;
        }

        public ItemFeather(SceneGame world, int count) : this(world)
        {
            Count = count;
        }

        [Construct("feather")]
        public static ItemFeather Construct(Context context)
        {
            return new ItemFeather(context.World);
        }

        public override Item CopyWithCount(int count)
        {
            return new ItemFeather(World, count);
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            var ingot = SpriteLoader.Instance.AddSprite("content/item_feather");
            scene.DrawSprite(ingot, 0, position - ingot.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
    }

    class Ingot : Item, IOre, IFuel
    {
        public override string BaseName { get => $"{Material.Name} Ingot"; set { } }
        public override string InventoryName => $"{Name} [{Count}]";

        public Material Material
        {
            get;
            set;
        }
        public int Count;
        public int Amount => Count * 200;
        public double FuelTemperature => Material.FuelTemperature;

        public Ingot(SceneGame world) : base(world, "Ingot", string.Empty)
        {
        }

        public Ingot(SceneGame world, Material material, int count) : this(world)
        {
            Material = material;
            Count = count;
        }

        [Construct("ingot")]
        public static Ingot Construct(Context context)
        {
            return new Ingot(context.World);
        }

        public bool CanUseInAnvil(PartType partType)
        {
            return Material.IsPartValid(partType);
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

        public override JToken WriteJson(Context context)
        {
            JToken json = base.WriteJson(context);
            json["material"] = Material.ID;
            json["count"] = Count;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Material = Material.GetMaterial(json["material"].Value<string>());
            Count = json["count"].Value<int>();
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

        public double DurabilityMax => Math.Floor(this.GetStat(Stat.Durability));
        public double Durability => Math.Max(0, Math.Floor(DurabilityMax - this.GetTotalDamage()));

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
            var itemBlock = GetEffects<Effect>();
            AddStatBlock(ref statBlock, itemBlock);

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
            var effectGroups = effects.SplitEffects<Effect>().GroupBy(effect => effect, Effect.StatEquality);

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

        public override JToken WriteJson(Context context)
        {
            JToken json = base.WriteJson(context);
            JArray materialArray = new JArray();
            foreach(var material in Materials)
            {
                materialArray.Add(material.ID);
            }
            json["materials"] = materialArray;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            JArray materialArray = json["materials"] as JArray;
            int i = 0;
            foreach (var materialJson in materialArray)
            {
                Materials[i] = Material.GetMaterial(materialJson.Value<string>());
                i++;
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

        [Construct("tool_blade")]
        public static ToolBlade Construct(Context context)
        {
            return new ToolBlade(context.World);
        }

        public static ToolBlade Create(SceneGame world, params Material[] materials)
        {
            ToolBlade tool = new ToolBlade(world);
            tool.SetMaterial(BLADE, materials[0]);
            tool.SetMaterial(GUARD, materials[1]);
            tool.SetMaterial(HANDLE, materials[2]);
            return tool;
        }

        public static string GetNickname(params Material[] materials)
        {
            string typeName;
            Material bladeMaterial = materials[0];
            switch(bladeMaterial.Parts[Blade].Sprite)
            {
                case ("cleave"):
                    typeName = "Cleaver";
                    break;
                case ("rip"):
                    typeName = "Ripper";
                    break;
                case ("disembowel"):
                    typeName = "Scalpel";
                    break;
                case ("flat"):
                    typeName = "Sword";
                    break;
                default:
                    typeName = "Blade";
                    break;
            }
            return $"{bladeMaterial.Name} {typeName}";
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

        [Construct("tool_adze")]
        public static ToolAdze Construct(Context context)
        {
            return new ToolAdze(context.World);
        }

        public static ToolAdze Create(SceneGame world, params Material[] materials)
        {
            ToolAdze tool = new ToolAdze(world);
            tool.SetMaterial(HEAD, materials[0]);
            tool.SetMaterial(BINDING, materials[1]);
            tool.SetMaterial(HANDLE, materials[2]);
            return tool;
        }

        public static string GetNickname(params Material[] materials)
        {
            string typeName;
            Material headMaterial = materials[0];
            switch (headMaterial.Parts[Head].Sprite)
            {
                case ("fork"):
                    typeName = "Trident";
                    break;
                case ("pick"):
                    typeName = "Mattock";
                    break;
                case ("sledge"):
                    typeName = "Sledge";
                    break;
                case ("reap"):
                    typeName = "Reaper";
                    break;
                default:
                    typeName = "Adze";
                    break;
            }
            return $"{headMaterial.Name} {typeName}";
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

        [Construct("tool_plate")]
        public static ToolPlate Construct(Context context)
        {
            return new ToolPlate(context.World);
        }

        public static ToolPlate Create(SceneGame world, params Material[] materials)
        {
            ToolPlate tool = new ToolPlate(world);
            tool.SetMaterial(CORE, materials[0]);
            tool.SetMaterial(COMPOSITE, materials[1]);
            tool.SetMaterial(TRIM, materials[2]);
            return tool;
        }

        public static string GetNickname(params Material[] materials)
        {
            string typeName;
            Material compositeMaterial = materials[0];
            switch (compositeMaterial.Parts[Composite].Sprite)
            {
                default:
                    typeName = "Plate";
                    break;
            }
            return $"{compositeMaterial.Name} {typeName}";
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

        [Construct("tool_arrow")]
        public static ToolArrow Construct(Context context)
        {
            return new ToolArrow(context.World);
        }

        public static ToolArrow Create(SceneGame world, params Material[] materials)
        {
            ToolArrow arrow = new ToolArrow(world);
            arrow.SetMaterial(TIP, materials[0]);
            arrow.SetMaterial(LIMB, materials[1]);
            arrow.SetMaterial(FLETCHING, materials[2]);
            return arrow;
        }

        public static string GetNickname(params Material[] materials)
        {
            string typeName;
            Material tipMaterial = materials[0];
            switch (tipMaterial.Parts[Tip].Sprite)
            {
                case ("bomb"):
                    typeName = "Bomb Arrow";
                    break;
                case ("fork"):
                    typeName = "Pronged Arrow";
                    break;
                case ("small"):
                    typeName = "Bolt";
                    break;
                case ("tip"):
                    typeName = "Broadhead Arrow";
                    break;
                default:
                    typeName = "Arrow";
                    break;
            }
            return $"{tipMaterial.Name} {typeName}";
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

        internal bool CanCollide(Skills.Projectile projectile, Tile tile)
        {
            return Skills.Projectile.CollideSolid(projectile, tile);
        }

        public IEnumerable<Wait> Impact(Skills.Projectile projectile, Tile tile)
        {
            Point velocity = projectile.Shooter.Facing.ToOffset();
            List<Wait> waits = new List<Wait>();
            foreach (Creature creature in tile.Creatures)
            {
                var wait = projectile.Shooter.Attack(creature, new Vector2(velocity.X, velocity.Y), ArrowAttack);
                waits.Add(wait);
            }
            yield return new WaitAll(waits);
        }

        public Attack ArrowAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(100, 0, 1);
            attack.ExtraEffects.Add(new AttackProjectile(this));
            foreach (var element in this.GetElements())
            {
                attack.Elements.Add(element.Key, element.Value);
            }
            return attack;
        }

        public IEnumerable<Wait> RoutineShoot(Creature creature, int dx, int dy, List<Wait> waitForDamage)
        {
            double volley = Math.Max(1,creature.GetStat(Stat.ArrowVolley));
            volley = 1;
            int distance = Math.Max(1, (int)creature.GetStat(Stat.ArrowRange));
            distance = 8;
            for (int i = 0; i < volley; i++)
            {
                Bullet bullet = new BulletArrow(creature.World, this, Vector2.Zero, ColorMatrix.Identity, 0);
                var projectile = new Skills.Projectile(bullet);
                projectile.ExtraEffects.Add(new Skills.ProjectileImpactAttack(ArrowAttack));
                projectile.ExtraEffects.Add(new Skills.ProjectileCollideSolid());
                creature.OnShoot(new ShootEvent(projectile, creature, creature.Tile));
                waitForDamage.Add(Scheduler.Instance.RunAndWait(projectile.ShootStraight(creature, creature.Tile, new Point(dx, dy), 3, distance)));
                this.TakeDamage(1, Element.Bludgeon, null);
                yield return new WaitTime(5);
            }
        }
    }

    abstract class AbstractShopItem : Item
    {
        public AbstractShopItem(SceneGame world, string name, string description) : base(world, name, description)
        {
        }

        public abstract double GetCost(Creature player);

        public abstract void Purchase(Creature player);

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            base.AddActions(ui, player, selection);
            double cost = GetCost(player);
            selection.Add(new ActAction($"Buy {Game.FormatIcon(this)}{BaseName} ({cost} EXP)", Description, () =>
            {
                Purchase(player);
                player.Experience -= GetCost(player);
                selection.Close();
            }, () => player.Experience >= GetCost(player)));
        }
    }

    class ShopRefillHealthFull : AbstractShopItem
    {
        public ShopRefillHealthFull(SceneGame world) : base(world, "Full Life", "Restores all HP")
        {
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            var ingot = SpriteLoader.Instance.AddSprite("content/item_potion_pink");
            scene.DrawSprite(ingot, 0, position - ingot.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }

        public override double GetCost(Creature player)
        {
            double missingHealth = player.GetStat(Stat.HP) - player.CurrentHP;

            return missingHealth;
        }

        public override void Purchase(Creature player)
        {
            player.Heal(player.GetStat(Stat.HP));
        }
    }

    class ShopRefillHealth : AbstractShopItem
    {
        public ShopRefillHealth(SceneGame world) : base(world, "Refill Health", "Consumes all EXP to restore as much HP as possible")
        {
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            var ingot = SpriteLoader.Instance.AddSprite("content/item_potion_blue");
            scene.DrawSprite(ingot, 0, position - ingot.Middle, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }

        public override double GetCost(Creature player)
        {
            return player.Experience;
        }

        public override void Purchase(Creature player)
        {
            player.Heal(player.Experience);
        }
    }

    class BurningBog : Item
    {
        Random Random = new Random();
        int OffsetX, OffsetY;
        public Slider Duration;

        public BurningBog(SceneGame world) : base(world, "Burning Bog", "Releases burning clouds every turn")
        {
            AddNormalTurn();
        }

        public override Wait NormalTurn(Turn turn)
        {
            Proc();
            Duration += 1;
            if (Duration.Done)
                this.Destroy();
            return Wait.NoWait;
        }

        public void Proc()
        {
            CloudFire cloud = Map.AddCloud(map => new CloudFire(map));
            foreach (var tile in SkillUtil.GetCircularArea(Tile, 1))
            {
                cloud.Add(tile, 5);
            }
        }

        public override void DrawIcon(SceneGame scene, Vector2 position)
        {
            //NOOP
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            SpriteReference gunpowder = SpriteLoader.Instance.AddSprite("content/gunpowder");
            if (Random.NextDouble() < 0.1)
            {
                OffsetX = Random.Next(gunpowder.Width);
                OffsetY = Random.Next(gunpowder.Height);
            }
            Tile tile = Tile;
            if (tile != null)
                scene.SpriteBatch.Draw(gunpowder.Texture, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8) - gunpowder.Middle, new Rectangle(OffsetX, OffsetY, gunpowder.Width, gunpowder.Height), Color.White);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectLowAdditive;
        }

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            //NOOP
        }

        public override void AddItemActions(InventoryItemList inventory, Creature player, MenuTextSelection selection)
        {
            //NOOP
        }

        public override JToken WriteJson(Context context)
        {
            JToken json = base.WriteJson(context);
            json["duration"] = Duration.WriteJson();
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Duration = new Slider(json["duration"]);
        }
    }
}
