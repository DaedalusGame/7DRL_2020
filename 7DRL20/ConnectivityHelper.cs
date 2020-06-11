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
                return Connectivity.GetBlobTile();
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
            if (a != null)
                a.Connectivity |= connection;
            if (b != null)
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
