﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    abstract class SpreadTile
    {
        public MapGenerator Generator;
        public GeneratorCell Cell;
        public GeneratorCell Origin;

        protected SpreadTile(GeneratorCell origin)
        {
            Origin = origin;
        }

        public abstract void Spread(Random random);
    }

    class SpreadCave : SpreadTile
    {
        bool WallOutside;
        int Distance;
        float Chance;
        GeneratorTile Floor;
        GeneratorTile Wall;

        public SpreadCave(GeneratorCell origin, int distance, GeneratorTile floor, GeneratorTile wall, bool wallOutside = true, float chance = 0.6f) : base(origin)
        {
            Distance = distance;
            WallOutside = wallOutside;
            Chance = chance;
            Floor = floor;
            Wall = wall;
        }

        public override void Spread(Random random)
        {
            Cell.Tile = GeneratorTile.Floor;

            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty)
                    {
                        tile.Tile = Wall;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                bool oneEmpty = false;
                foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle(random))
                {
                    if (tile.Room == null)
                    {
                        if (index <= 0 || Generator.Random.NextDouble() > Chance)
                        {
                            if (Origin.Room != null)
                                tile.Room = Origin.Room;
                            tile.Tile = Floor;
                            tile.Group = Cell.Group;
                            tile.AddSpread(new SpreadCave(Origin, Distance - 1, Floor, Wall, WallOutside, Chance));
                        }
                        else
                        {
                            oneEmpty = true;
                        }
                    }
                    index++;
                }
                if (oneEmpty)
                {
                    Cell.AddSpread(new SpreadCave(Origin, Distance - 1, Floor, Wall, WallOutside, Chance));
                }
            }
            //Cell.Tile = GeneratorTile.Floor;
        }
    }

    class SpreadTower : SpreadTile
    {
        bool WallOutside;
        bool IsRoom;
        int Distance;
        float Radius;
        GeneratorTile Floor;
        GeneratorTile Wall;

        public SpreadTower(GeneratorCell origin, int distance, float radius, GeneratorTile floor, GeneratorTile wall, bool wallOutside = true, bool isRoom = true) : base(origin)
        {
            Distance = distance;
            Radius = radius;
            WallOutside = wallOutside;
            IsRoom = isRoom;
            Floor = floor;
            Wall = wall;
        }

        public override void Spread(Random random)
        {
            Cell.Tile = GeneratorTile.FloorBrick;
            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty && WallOutside)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = Wall;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle(random))
                {
                    int dx = tile.X - Origin.X;
                    int dy = tile.Y - Origin.Y;
                    if (tile.Room == null)
                    {
                        if (dx * dx + dy * dy < Radius * Radius)
                        {
                            if (IsRoom && Origin.Room != null)
                                tile.Room = Origin.Room;
                            tile.Tile = Floor;
                            tile.Group = Cell.Group;
                            tile.AddSpread(new SpreadTower(Origin, Distance - 1, Radius, Floor, Wall, WallOutside, IsRoom));
                        }
                        else if (tile.Tile == GeneratorTile.Empty && WallOutside)
                        {
                            if (IsRoom && Origin.Room != null)
                                tile.Room = Origin.Room;
                            tile.Tile = Wall;
                            tile.Group = Cell.Group;
                        }
                    }
                    index++;
                }
            }
        }
    }

    class SpreadCastle : SpreadTile
    {
        bool WallOutside;
        bool IsRoom;
        int Distance;
        GeneratorTile Floor;
        GeneratorTile Wall;

        public SpreadCastle(GeneratorCell origin, int distance, GeneratorTile floor, GeneratorTile wall, bool isRoom = true) : base(origin)
        {
            Distance = distance;
            IsRoom = isRoom;
            Floor = floor;
            Wall = wall;
        }

        public override void Spread(Random random)
        {
            Cell.Tile = Floor;
            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty && Wall != GeneratorTile.Empty)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = Wall;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle(random))
                {
                    if (tile.Room == null)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = Floor;
                        tile.Group = Cell.Group;
                        tile.AddSpread(new SpreadCastle(Origin, Distance - 1, Floor, Wall, IsRoom));
                    }
                    index++;
                }
            }
        }
    }

    class SpreadVault : SpreadTile
    {
        bool IsRoom;
        int Distance;
        GeneratorTile Floor;
        GeneratorTile Wall;

        public SpreadVault(GeneratorCell origin, int distance, GeneratorTile floor, GeneratorTile wall, bool isRoom = true) : base(origin)
        {
            Distance = distance;
            IsRoom = isRoom;
            Floor = floor;
            Wall = wall;
        }

        public override void Spread(Random random)
        {
            Cell.Tile = Floor;
            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty && Wall != GeneratorTile.Empty)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = Wall;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle(random))
                {
                    if (tile.Tile == GeneratorTile.Empty && tile.Room == null)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = Floor;
                        tile.Group = Cell.Group;
                        tile.AddSpread(new SpreadVault(Origin, Distance - 1, Floor, Wall, IsRoom));
                    }
                    index++;
                }
            }
        }
    }

    class SpreadStatues : SpreadTile
    {
        int Distance;

        int GridHorizontal;
        int GridVertical;

        GeneratorTile Tile;

        public SpreadStatues(GeneratorCell origin, int distance, int gridHorizontal, int gridVertical, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            GridHorizontal = gridHorizontal;
            GridVertical = gridVertical;
            Tile = tile;
        }

        public override void Spread(Random random)
        {
            if (Distance <= 0)
                return;

            int index = 0;

            foreach (var tile in Cell.GetStatueNeighbors(GridHorizontal, GridVertical).Where(x => x != null).Where(x => x.Tile == GeneratorTile.Floor).Shuffle(random))
            {
                if (tile.Tile == GeneratorTile.Floor)
                {
                    tile.Tile = Tile;
                    tile.AddSpread(new SpreadStatues(Origin, Distance - 1, GridHorizontal, GridVertical, Tile));
                }
                index++;
            }
        }
    }

    class SpreadGlow : SpreadTile
    {
        int Distance;
        float Chance;
        bool Glowing;

        public SpreadGlow(GeneratorCell origin, int distance, float chance, bool glowing) : base(origin)
        {
            Distance = distance;
            Chance = chance;
            Glowing = glowing;
        }

        public override void Spread(Random random)
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle(random))
            {
                if (tile.Glowing != Glowing)
                {
                    if (index <= 0 || Generator.Random.NextDouble() > Chance)
                    {
                        tile.Glowing = Glowing;
                        tile.AddSpread(new SpreadGlow(Origin, Distance - 1, Chance, Glowing));
                    }
                    else
                    {
                        oneEmpty = true;
                    }
                }
                index++;
            }
            if (oneEmpty)
            {
                Cell.AddSpread(new SpreadGlow(Origin, Distance - 1, Chance, Glowing));
            }
        }
    }

    class SpreadCarpet : SpreadTile
    {
        int Distance;
        GeneratorTile Tile;

        public SpreadCarpet(GeneratorCell origin, int distance, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            Tile = tile;
        }

        public override void Spread(Random random)
        {
            int index = 0;
            if(Distance > 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle(random))
                {
                    if (tile.Tile.HasTag(TileTag.Floor))
                    {
                        tile.Tile = Tile;
                        tile.Group = Cell.Group;
                        tile.AddSpread(new SpreadCarpet(Origin, Distance - 1, Tile));
                    }
                    index++;
                }
            }
        }
    }

    class SpreadLake : SpreadTile
    {
        int Distance;
        float Chance;
        GeneratorTile Tile;

        public SpreadLake(GeneratorCell origin, int distance, float chance, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            Chance = chance;
            Tile = tile;
        }

        public override void Spread(Random random)
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle(random))
            {
                if (tile.Tile.HasTag(TileTag.Floor))
                {
                    if (index <= 0 || Generator.Random.NextDouble() > Chance)
                    {
                        tile.Tile = Tile;
                        tile.AddSpread(new SpreadLake(Origin, Distance - 1, Chance, Tile));
                    }
                    else
                    {
                        oneEmpty = true;
                    }
                }
                index++;
            }
            if (oneEmpty)
            {
                Cell.AddSpread(new SpreadLake(Origin, Distance - 1, Chance, Tile));
            }
        }
    }

    class SpreadPlatform : SpreadTile
    {
        int Distance;
        GeneratorTile Tile;

        public SpreadPlatform(GeneratorCell origin, int distance, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            Tile = tile;
        }

        public override void Spread(Random random)
        {
            int index = 0;
            if (Distance > 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle(random))
                {
                    if (tile.Tile.HasTag(TileTag.Liquid) && tile.Tile != Tile)
                    {
                        tile.Tile = Tile;
                        tile.Group = Cell.Group;
                        tile.AddSpread(new SpreadPlatform(Origin, Distance - 1, Tile));
                    }
                    index++;
                }
            }
        }
    }

    class SpreadDeepLake : SpreadTile
    {
        int Distance;
        float Chance;
        GeneratorTile Tile;

        public SpreadDeepLake(GeneratorCell origin, int distance, float chance, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            Chance = chance;
            Tile = tile;
        }

        public override void Spread(Random random)
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle(random))
            {
                if (tile.Tile.HasTag(TileTag.Liquid) && tile.Tile != Tile)
                {
                    if (index <= 0 || Generator.Random.NextDouble() > Chance)
                    {
                        tile.Tile = Tile;
                        tile.AddSpread(new SpreadDeepLake(Origin, Distance - 1, Chance, Tile));
                    }
                    else
                    {
                        oneEmpty = true;
                    }
                }
                index++;
            }
            if (oneEmpty)
            {
                Cell.AddSpread(new SpreadDeepLake(Origin, Distance - 1, Chance, Tile));
            }
        }
    }

    class SpreadPlant : SpreadTile
    {
        int Distance;
        float Chance;
        GeneratorTile Tile;

        public SpreadPlant(GeneratorCell origin, int distance, float chance, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            Chance = chance;
            Tile = tile;
        }

        public override void Spread(Random random)
        {
            if (Distance <= 0)
                return;

            int index = 0;

            foreach (var tile in Cell.GetPlantNeighbors().Where(x => x != null).Where(x => x.Tile == GeneratorTile.Floor).Shuffle(random))
            {
                if (index > 0 && Generator.Random.NextDouble() > Chance)
                    break;

                if (tile.Tile == GeneratorTile.Floor)
                {
                    tile.Tile = Tile;
                    tile.AddSpread(new SpreadPlant(Origin, Distance - 1, Chance, Tile));
                }
                index++;
            }
        }
    }

    class SpreadOre : SpreadTile
    {
        int Distance;
        float Chance;
        GeneratorTile Tile;

        public SpreadOre(GeneratorCell origin, int distance, float chance, GeneratorTile tile) : base(origin)
        {
            Distance = distance;
            Chance = chance;
            Tile = tile;
        }

        private bool IsNaturalWall(GeneratorCell cell)
        {
            return cell.Tile.HasTag(TileTag.Wall) && !cell.Tile.HasTag(TileTag.Artificial);
        }

        public override void Spread(Random random)
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle(random))
            {
                if (IsNaturalWall(tile))
                {
                    if (index <= 0 || Generator.Random.NextDouble() > Chance)
                    {
                        tile.Tile = Tile;
                        tile.AddSpread(new SpreadOre(Origin, Distance - 1, Chance, Tile));
                    }
                    else
                    {
                        oneEmpty = true;
                    }
                }
                index++;
            }
            if (oneEmpty)
            {
                Cell.AddSpread(new SpreadOre(Origin, Distance - 1, Chance, Tile));
            }
        }
    }
}
