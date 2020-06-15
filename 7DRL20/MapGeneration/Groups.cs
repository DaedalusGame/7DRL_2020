using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    abstract class GeneratorGroup
    {
        protected MapGenerator Generator;
        protected HashSet<GeneratorGroup> ConnectedGroups = new HashSet<GeneratorGroup>();
        protected HashSet<GeneratorCell> Cells = new HashSet<GeneratorCell>();
        protected IEnumerable<RoomGroup> Rooms => GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

        public Color Color;
        public TileColor CaveColor;
        public TileColor BrickColor;
        public TileColor WoodColor;
        public Func<float, Color> GlowColor = (slide) => Color.TransparentBlack;
        public ColorMatrix Atmosphere = ColorMatrix.Identity;
        public List<EnemySpawn> Spawns = new List<EnemySpawn>();
        public GroupGenerator Template;

        protected GeneratorGroup(MapGenerator generator)
        {
            Generator = generator;
        }

        public IEnumerator<Action> Techniques;

        public void Connect(GeneratorGroup other)
        {
            this.ConnectedGroups.Add(other);
            other.ConnectedGroups.Add(this);
        }

        public bool IsConnected(GeneratorGroup other)
        {
            return ConnectedGroups.Contains(other);
        }

        public abstract void PlaceConnection(MapGenerator generator, GeneratorCell cell);

        public abstract void PlaceRoom(MapGenerator generator, GeneratorCell cell);

        public abstract IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b);

        public IEnumerable<GeneratorCell> GetCells()
        {
            return Cells.ToList();
        }

        public void Add(GeneratorCell generatorCell)
        {
            Cells.Add(generatorCell);
        }

        public void Remove(GeneratorCell generatorCell)
        {
            Cells.Remove(generatorCell);
        }

        private Rectangle Between(Point a, Point b)
        {
            return new Rectangle(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        protected virtual IEnumerator<Action> GetTechniques()
        {
            return Enumerable.Empty<Action>().GetEnumerator();
        }

        public void StartTechniques()
        {
            Techniques = GetTechniques();
        }

        public void RunTechnique()
        {
            if (Techniques != null)
            {
                if (Techniques.MoveNext())
                    Techniques.Current();
                else
                    Techniques = null;
            }
        }

        #region Techniques
        protected void MakeLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile == GeneratorTile.Wall));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.Lava));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Lava));
            }
            Generator.Expand();
        }

        protected void MakeSuperLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Lava);
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadDeepLake(null, 10, 0.8f, GeneratorTile.SuperLava));
            }
            Generator.Expand();
        }

        protected void MakeHyperLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.SuperLava);
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadDeepLake(null, 5, 0.8f, GeneratorTile.HyperLava));
            }
            Generator.Expand();
        }

        protected void ShatterFloor()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile.HasTag(TileTag.Floor) && cell.Tile.HasTag(TileTag.Artificial)).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile.HasTag(TileTag.Liquid)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.Floor));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Floor));
            }
            Generator.Expand();
        }

        protected void MakeCarpets(int count, int size, GeneratorTile carpet, Func<GeneratorCell,bool> predicate)
        {
            var validArea = GetCells().Where(predicate);
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(count))
            {
                cell.AddSpread(new SpreadCarpet(null, size, carpet));
            }

            Generator.Expand();
        }

        protected void ConnectCarpets()
        {
            bool isCarpet(GeneratorCell cell) => cell != null && cell.Tile == GeneratorTile.Carpet;

            var carpets = GetCells().Where(cell => cell.Tile == GeneratorTile.Carpet);
            foreach (var carpet in carpets)
            {
                carpet.Connectivity = Connectivity.None;
                if (isCarpet(carpet.GetNeighbor(0, 1)))
                    carpet.Connectivity |= Connectivity.South;
                if (isCarpet(carpet.GetNeighbor(1, 0)))
                    carpet.Connectivity |= Connectivity.East;
                if (isCarpet(carpet.GetNeighbor(0, -1)))
                    carpet.Connectivity |= Connectivity.North;
                if (isCarpet(carpet.GetNeighbor(-1, 0)))
                    carpet.Connectivity |= Connectivity.West;
                if (isCarpet(carpet.GetNeighbor(1, 1)))
                    carpet.Connectivity |= Connectivity.SouthEast;
                if (isCarpet(carpet.GetNeighbor(1, -1)))
                    carpet.Connectivity |= Connectivity.NorthEast;
                if (isCarpet(carpet.GetNeighbor(-1, -1)))
                    carpet.Connectivity |= Connectivity.NorthWest;
                if (isCarpet(carpet.GetNeighbor(-1, 1)))
                    carpet.Connectivity |= Connectivity.SouthWest;
            }
        }

        protected void MakeDarkLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile.HasTag(TileTag.Floor)).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile.HasTag(TileTag.Wall)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.DarkLava));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.DarkLava));
            }
            Generator.Expand();
        }

        protected void MakeAcidLakes()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile == GeneratorTile.Wall));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.AcidPool));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.AcidPool));
            }
            Generator.Expand();
        }

        protected void MakeWaterLakes()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile.HasTag(TileTag.Wall)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.Water));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Water));
            }
            Generator.Expand();
        }

        protected void MakeWaterShallows()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile == GeneratorTile.Water));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.WaterShallow));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.WaterShallow));
            }
            Generator.Expand();
        }

        protected void MakeCoral(GeneratorTile coral)
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile.HasTag(TileTag.Liquid)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadPlant(null, 5, 0.0f, coral));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadPlant(null, 3, 0.0f, coral));
            }
            Generator.Expand();
        }

        protected void MakeBridges()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                foreach (var connection in room.Connections)
                {
                    var path = GetPath(Generator, new Point(room.Origin.X, room.Origin.Y), new Point(connection.X, connection.Y));
                    foreach (var cell in path.Select(Generator.GetCell))
                    {
                        if (cell.Tile.HasTag(TileTag.Liquid))
                        {
                            cell.Tile = GeneratorTile.Bridge;
                        }
                    }
                    Generator.WaitSoft();
                }
            }
        }

        protected void MakeStoneBridges()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                foreach (var connection in room.Connections)
                {
                    var path = GetPath(Generator, new Point(room.Origin.X, room.Origin.Y), new Point(connection.X, connection.Y));
                    if(Generator.Random.NextDouble() < 0.6)
                    foreach (var cell in path.Select(Generator.GetCell))
                    {
                        if (cell.Tile.HasTag(TileTag.Liquid))
                        {
                            cell.Tile = GeneratorTile.FloorBrick;
                            cell.AddSpread(new SpreadPlatform(null, 2, GeneratorTile.FloorBrick));
                        }
                    }
                    Generator.WaitSoft();
                }
            }

            Generator.Expand();
        }

        protected void MakeGlowingFloor()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile.HasTag(TileTag.Liquid)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms * 3))
            {
                cell.AddSpread(new SpreadGlow(null, 5, 0.6f, true));
            }
            Generator.Expand();
        }

        protected void MakeGlowingWall()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Wall).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile.HasTag(TileTag.Liquid)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms))
            {
                cell.AddSpread(new SpreadGlow(null, 5, 0.6f, true));
            }
            Generator.Expand();
        }

        protected void MakeOutsideRooms()
        {
            int rooms = Rooms.Count();
            var borderWalls = GetCells().Where(cell => cell.Tile.HasTag(TileTag.Wall)).Where(cell => cell.GetNeighbors().Where(neighbor => neighbor != null).Any(neighbor => neighbor.Tile.HasTag(TileTag.Floor))).ToHashSet();
            var hidden = Generator.GetAllCells().Where(cell => cell.Tile == GeneratorTile.Empty);
            var validArea = hidden.Where(cell => cell.GetNeighbors().Any(neighbor => borderWalls.Contains(neighbor)));
            validArea = validArea.Shuffle();
            foreach (var cell in validArea.Take(rooms))
            {
                var border = cell.GetNeighbors().Intersect(borderWalls).ToList().Pick(Generator.Random); //Find a wall
                var room = border.GetNeighbors().Where(neighbor => neighbor.Room != null && neighbor.Group != null).FirstOrDefault();
                if (room != null)
                {
                    cell.Group = room.Group;
                    cell.AddSpread(new SpreadVault(room, 2, GeneratorTile.FloorBrick, GeneratorTile.WallBrick));
                    border.Tile = GeneratorTile.FloorBrick; //Punch a hole
                }
            }
            Generator.Expand();
        }
        #endregion
    }

    /*class Home : GeneratorGroup
    {
        public Home(MapGenerator generator) : base(generator)
        {
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            return Enumerable.Empty<Point>();
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
        {
            //NOOP
        }

        public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            for (int i = 0; i < 4; i++)
            {
                int offX = generator.Random.Next(-5, 6);
                int offY = generator.Random.Next(-5, 6);

                GeneratorCell center = cell.GetNeighbor(offX, offY);
                if (center.Tile == GeneratorTile.Empty)
                {
                    center.Tile = GeneratorTile.FloorBrick;
                    center.Group = this;
                    //generator.SpreadCastle(center, 3 + generator.Random.Next(4), GeneratorTile.FloorBrick);
                }
            }
        }
    }*/

    class Castle : GeneratorGroup
    {
        public Castle(MapGenerator generator) : base(generator)
        {
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            var dijkstraMap = Util.Dijkstra(a, b, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, generator.GetWeightStraight, generator.GetNeighbors);
            return dijkstraMap.FindPath(b);
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadCastle(null, 2, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, false));
        }

        public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            if (generator.Random.NextDouble() < 0.8)
                cell.AddSpread(new SpreadCastle(null, generator.Random.Next(3, 7), GeneratorTile.FloorBrick, GeneratorTile.WallBrick));
            else
                cell.AddSpread(new SpreadTower(null, generator.Random.Next(6, 10), generator.Random.Next(3, 6) + 0.5f));
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            yield return MakeStatues;
        }

        private void MakeStatues()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                if (Generator.Random.NextDouble() < 0.2)
                    room.Origin.AddSpread(new SpreadStatues(null, 3, Generator.Random.Next(3, 5), Generator.Random.Next(3, 5), GeneratorTile.Statue));
            }

            Generator.Expand();
        }
    }

    class Tower : GeneratorGroup
    {
        public Tower(MapGenerator generator) : base(generator)
        {
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            var dijkstraMap = Util.Dijkstra(a, b, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, generator.GetWeightStraight, generator.GetAllNeighbors);
            return dijkstraMap.FindPath(b);
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadCastle(null, 2, GeneratorTile.FloorBrick, GeneratorTile.Empty, false));
        }

        public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadTower(null, generator.Random.Next(6, 10), generator.Random.Next(3, 6) + 0.5f));
        }
    }

    class Cave : GeneratorGroup
    {
        public Cave(MapGenerator generator) : base(generator)
        {
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            var dijkstraMap = Util.Dijkstra(a, b, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, generator.GetWeightWavy, generator.GetNeighbors);
            return dijkstraMap.FindPath(b);

            //return Util.DrunkardWalk(a, b, generator.GetNeighbors, generator.Random);
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadCave(null, generator.Random.Next(2, 5)));
            //generator.SpreadCave(cell, 2);
        }

        public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadCave(null, generator.Random.Next(3, 10)));
            //generator.SpreadCave(cell, generator.Random.Next(3, 10));
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            //yield return MakeLava;
            //yield return MakeSuperLava;
            //yield return MakeHyperLava;
            //yield return MakeAcidLakes;
            //yield return MakeCoral;
            //yield return MakeGlowingFloor;
            yield return MakeBridges;
        }
    }

    class CaveAcid : Cave
    {
        public CaveAcid(MapGenerator generator) : base(generator)
        {
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            yield return MakeAcidLakes;
            yield return () => MakeCoral(GeneratorTile.AcidCoral);
            yield return MakeGlowingFloor;
            yield return MakeGlowingWall;
            yield return MakeBridges;
        }
    }

    class CaveLava : Cave
    {
        public CaveLava(MapGenerator generator) : base(generator)
        {
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            yield return MakeLava;
            yield return MakeBridges;
        }
    }

    class CaveMagma : Cave
    {
        public CaveMagma(MapGenerator generator) : base(generator)
        {
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            yield return MakeLava;
            yield return MakeLava;
            yield return MakeSuperLava;
            yield return MakeSuperLava;
            yield return MakeHyperLava;
            yield return MakeHyperLava;
            yield return MakeBridges;
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadCave(null, generator.Random.Next(4, 6)));
        }

        public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            cell.AddSpread(new SpreadCave(null, generator.Random.Next(5, 13)));
        }
    }

    class CaveWater : Cave
    {
        public CaveWater(MapGenerator generator) : base(generator)
        {
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            yield return MakeWaterLakes;
            yield return MakeWaterShallows;
            yield return () => MakeCoral(GeneratorTile.Coral);
            yield return MakeGlowingFloor;
            yield return MakeBridges;
        }
    }

    class CastleDark : Castle
    {
        public CastleDark(MapGenerator generator) : base(generator)
        {
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            int rooms = Rooms.Count();
            yield return () => MakeCarpets(rooms / 3, 5, GeneratorTile.Carpet, cell => cell.Tile.HasTag(TileTag.Floor));
            yield return ConnectCarpets;
            yield return MakeDarkLava;
            yield return ShatterFloor;
            yield return MakeGlowingFloor;
            yield return MakeStoneBridges;
            yield return MakeOutsideRooms;
        }
    }

    class Home : Castle
    {
        public Home(MapGenerator generator) : base(generator)
        {
        }

        protected override IEnumerator<Action> GetTechniques()
        {
            int rooms = Rooms.Count();
            yield return () => MakeCarpets(rooms / 3, 5, GeneratorTile.Carpet, cell => cell.Tile == GeneratorTile.FloorPlank);
            yield return ConnectCarpets;
            yield return MakeOutsideRooms;
        }

        public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            if (generator.Random.NextDouble() < 0.5)
                cell.AddSpread(new SpreadCastle(null, generator.Random.Next(3, 6), GeneratorTile.FloorBrick, GeneratorTile.WallBrick));
            else
                cell.AddSpread(new SpreadCastle(null, generator.Random.Next(3, 6), GeneratorTile.FloorPlank, GeneratorTile.WallPlank));
        }
    }
}
