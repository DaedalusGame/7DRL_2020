using System;
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

        public abstract void Spread();
    }

    class SpreadCave : SpreadTile
    {
        bool WallOutside;
        int Distance;
        float Chance;

        public SpreadCave(GeneratorCell origin, int distance, bool wallOutside = true, float chance = 0.6f) : base(origin)
        {
            Distance = distance;
            WallOutside = wallOutside;
            Chance = chance;
        }

        public override void Spread()
        {
            Cell.Tile = GeneratorTile.Floor;

            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty)
                    {
                        tile.Tile = GeneratorTile.Wall;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                bool oneEmpty = false;
                foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle())
                {
                    if (tile.Room == null)
                    {
                        if (index <= 0 || Generator.Random.NextDouble() > Chance)
                        {
                            if (Origin.Room != null)
                                tile.Room = Origin.Room;
                            tile.Tile = GeneratorTile.Floor;
                            tile.Group = Cell.Group;
                            tile.AddSpread(new SpreadCave(Origin, Distance - 1, WallOutside, Chance));
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
                    Cell.AddSpread(new SpreadCave(Origin, Distance - 1, WallOutside, Chance));
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

        public SpreadTower(GeneratorCell origin, int distance, float radius, bool wallOutside = true, bool isRoom = true) : base(origin)
        {
            Distance = distance;
            Radius = radius;
            WallOutside = wallOutside;
            IsRoom = isRoom;
        }

        public override void Spread()
        {
            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty && WallOutside)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = GeneratorTile.WallBrick;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle())
                {
                    int dx = tile.X - Origin.X;
                    int dy = tile.Y - Origin.Y;
                    if (tile.Room == null)
                    {
                        if (dx * dx + dy * dy < Radius * Radius)
                        {
                            if (IsRoom && Origin.Room != null)
                                tile.Room = Origin.Room;
                            tile.Tile = GeneratorTile.Floor;
                            tile.Group = Cell.Group;
                            tile.AddSpread(new SpreadTower(Origin, Distance - 1, Radius, WallOutside, IsRoom));
                        }
                        else if (tile.Tile == GeneratorTile.Empty && WallOutside)
                        {
                            if (IsRoom && Origin.Room != null)
                                tile.Room = Origin.Room;
                            tile.Tile = GeneratorTile.WallBrick;
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

        public SpreadCastle(GeneratorCell origin, int distance, bool wallOutside = true, bool isRoom = true) : base(origin)
        {
            Distance = distance;
            WallOutside = wallOutside;
            IsRoom = isRoom;
        }

        public override void Spread()
        {
            int index = 0;
            if (Distance <= 1)
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null))
                {
                    if (tile.Tile == GeneratorTile.Empty && WallOutside)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = GeneratorTile.WallBrick;
                        tile.Group = Cell.Group;
                    }
                }
            }
            else
            {
                foreach (var tile in Cell.GetAllNeighbors().Where(x => x != null).Shuffle())
                {
                    if (tile.Room == null)
                    {
                        if (IsRoom && Origin.Room != null)
                            tile.Room = Origin.Room;
                        tile.Tile = GeneratorTile.Floor;
                        tile.Group = Cell.Group;
                        tile.AddSpread(new SpreadCastle(Origin, Distance - 1, WallOutside, IsRoom));
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

        public override void Spread()
        {
            if (Distance <= 0)
                return;

            int index = 0;

            foreach (var tile in Cell.GetStatueNeighbors(GridHorizontal, GridVertical).Where(x => x != null).Where(x => x.Tile == GeneratorTile.Floor).Shuffle())
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

        public override void Spread()
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle())
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

        public override void Spread()
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle())
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

        public override void Spread()
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle())
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

        public override void Spread()
        {
            if (Distance <= 0)
                return;

            int index = 0;

            foreach (var tile in Cell.GetPlantNeighbors().Where(x => x != null).Where(x => x.Tile == GeneratorTile.Floor).Shuffle())
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

        public override void Spread()
        {
            if (Distance <= 0)
                return;

            int index = 0;
            bool oneEmpty = false;

            foreach (var tile in Cell.GetNeighbors().Where(x => x != null).Shuffle())
            {
                if (tile.Tile == GeneratorTile.Wall || tile.Tile == GeneratorTile.Empty)
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
