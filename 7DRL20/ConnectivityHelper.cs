using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    [Flags]
    enum Connectivity
    {
        None = 0,
        //Directions
        North = 1,
        NorthEast = 2,
        East = 4,
        SouthEast = 8,
        South = 16,
        SouthWest = 32,
        West = 64,
        NorthWest = 128,
        //Kill Flags
        KillNorth = ~(North | NorthEast | NorthWest),
        KillEast = ~(East | NorthEast | SouthEast),
        KillSouth = ~(South | SouthEast | SouthWest),
        KillWest = ~(West | NorthWest | SouthWest),
    }

    class ConnectivityHelper
    {
        static Dictionary<int, int> BlobTileMap = new Dictionary<int, int>() //Mapper for the minimal tileset, index in memory -> index in image
        {
            {0, 0},
            {4, 1},
            {92, 2},
            {124, 3},
            {116, 4},
            {80, 5},
            //{0, 6},
            {16, 7},
            {20, 8},
            {87, 9},
            {223, 10},
            {241, 11},
            {21, 12},
            {64, 13},
            {29, 14},
            {117, 15},
            {85, 16},
            {71, 17},
            {221, 18},
            {125, 19},
            {112, 20},
            {31, 21},
            {253, 22},
            {113, 23},
            {28, 24},
            {127, 25},
            {247, 26},
            {209, 27},
            {23, 28},
            {199, 29},
            {213, 30},
            {95, 31},
            {255, 32},
            {245, 33},
            {81, 34},
            {5, 35},
            {84, 36},
            {93, 37},
            {119, 38},
            {215, 39},
            {193, 40},
            {17, 41},
            //{0, 42},
            {1, 43},
            {7, 44},
            {197, 45},
            {69, 46},
            {68, 47},
            {65, 48},
        };

        private Tile Tile;
        public Connectivity Connectivity;
        public bool Dirty = true;
        private Func<Tile, ConnectivityHelper> GetConnection;
        private Func<ConnectivityHelper, ConnectivityHelper, bool> Connects;

        private Map Map => Tile.Map;

        public ConnectivityHelper(Tile tile, Func<Tile, ConnectivityHelper> getConnection, Func<ConnectivityHelper, ConnectivityHelper, bool> connects)
        {
            Tile = tile;
            GetConnection = getConnection;
            Connects = connects;
        }

        public int BlobIndex
        {
            get
            {
                Connectivity connectivity = CullDiagonals();
                return BlobTileMap.ContainsKey((int)connectivity) ? BlobTileMap[(int)connectivity] : BlobTileMap[0];
            }
        }

        public void CalculateIfNeeded()
        {
            if(Dirty)
                Calculate();
        }

        public void Calculate()
        {
            ConnectivityHelper south = GetNeighbor(0, 1);
            ConnectivityHelper east = GetNeighbor(1, 0);
            ConnectivityHelper southeast = GetNeighbor(1, 1);
            bool doEast = true;
            bool doSouth = true;

            if (doEast && Connects(this,east))
                Connect(this, east, Connectivity.East);
            if (doSouth && Connects(this, south))
                Connect(this, south, Connectivity.South);
            if (doEast && doSouth)
            {
                if (Connects(this, southeast))
                    Connect(this, southeast, Connectivity.SouthEast);
                if (Connects(east, south))
                    Connect(east, south, Connectivity.SouthWest);
            }

            Dirty = false;
        }

        private Connectivity CullDiagonals()
        {
            Connectivity connectivity = Connectivity;
            if (!connectivity.HasFlag(Connectivity.North))
                connectivity &= Connectivity.KillNorth;
            if (!connectivity.HasFlag(Connectivity.East))
                connectivity &= Connectivity.KillEast;
            if (!connectivity.HasFlag(Connectivity.South))
                connectivity &= Connectivity.KillSouth;
            if (!connectivity.HasFlag(Connectivity.West))
                connectivity &= Connectivity.KillWest;
            return connectivity;
        }

        public void Clear() //Bulky?
        {
            ConnectivityHelper north = GetNeighbor(0, -1);
            ConnectivityHelper west = GetNeighbor(-1, 0);
            ConnectivityHelper northwest = GetNeighbor(-1, -1);

            Disconnect(this, north, Connectivity.North);
            Disconnect(this, west, Connectivity.West);
            Disconnect(this, northwest, Connectivity.NorthWest);
            Disconnect(this, GetNeighbor(1, -1), Connectivity.NorthEast);
            Disconnect(this, GetNeighbor(1, 0), Connectivity.East);
            Disconnect(this, GetNeighbor(1, 1), Connectivity.SouthEast);
            Disconnect(this, GetNeighbor(0, 1), Connectivity.South);
            Disconnect(this, GetNeighbor(-1, 1), Connectivity.SouthWest);
            MarkDirty();
            north?.MarkDirty();
            west?.MarkDirty();
            northwest?.MarkDirty();
        }

        private ConnectivityHelper GetNeighbor(int dx, int dy)
        {
            Tile tile = Tile.GetNeighbor(dx, dy);
            return GetConnection(tile);
        }

        private void MarkDirty()
        {
            Dirty = true;
        }

        public static void Connect(ConnectivityHelper a, ConnectivityHelper b, Connectivity connection)
        {
            Connectivity rotated = connection.Rotate(4);
            if(a != null)
                a.Connectivity |= connection;
            if(b != null)
                b.Connectivity |= rotated;
        }

        public static void Disconnect(ConnectivityHelper a, ConnectivityHelper b, Connectivity connection)
        {
            Connectivity rotated = connection.Rotate(4);
            if (a != null)
                a.Connectivity &= ~connection;
            if (b != null)
                b.Connectivity &= ~rotated;
        }
    }
}
