﻿using RoguelikeEngine.Effects;
using RoguelikeEngine.MapGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class MapTile : IEffectHolder
    {
        public class FakeOutside : MapTile //Subtype that handles the tiles outside the map.
        {
            public override ReusableID ObjectID
            {
                get
                {
                    return ReusableID.Null;
                }
                set
                {
                    //NOOP
                }
            }

            public FakeOutside(Map map, int x, int y) : base(map, x, y, new Tile.FakeOutside(map,x,y))
            {
            }
        }

        public virtual ReusableID ObjectID
        {
            get;
            set;
        }

        public SceneGame World => Map.World;
        public Map Map;
        public int X, Y;

        public Tile Tile;
        public Tile UnderTile;

        public GeneratorGroup Group;
        public bool Glowing;

        public MapTile(Map map, int x, int y)
        {
            ObjectID = EffectManager.SetID(this);
            Map = map;
            X = x;
            Y = y;
        }

        public MapTile(Map map, int x, int y, Tile tile) : this(map,x,y)
        {
            Set(tile);
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public void Set(Tile tile)
        {
            Tile = tile;
            if (Tile != null)
                Tile.SetParent(this);
        }

        public void SetUnder(Tile tile)
        {
            UnderTile = tile;
            if(UnderTile != null)
                UnderTile.SetParent(this);
        }

        public void SaveUnder()
        {
            UnderTile = Tile;
        }

        public void RestoreUnder()
        {
            Tile = UnderTile;
        }

        public override string ToString()
        {
            return $"{Tile} ({X},{Y})";
        }
    }
}
