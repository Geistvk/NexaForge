using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace NexaForge
{
    // Ein einfaches 2D-Bauraster (X/Z), auf dem Gebäude platziert werden können.
    // Enthält zusätzlich eine prozedural erzeugte Erz-Schicht (endliche Ressourcen).
    public class VoxelGrid
    {
        public int Width { get; }
        public int Depth { get; }
        public float CellSize { get; }

        private readonly Dictionary<(int x, int z), object> _cells = new();
        private readonly float[,] _oreDeposits;

        public VoxelGrid(int width, int depth, float cellSize)
        {
            Width = width;
            Depth = depth;
            CellSize = cellSize;
            _oreDeposits = new float[width, depth];
        }

        // Erzeugt prozedural verteilte Erzvorkommen (unregelmäßige Flecken statt Gleichverteilung)
        public void GenerateOreDeposits(int seed)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    float n = NoiseGenerator.SmoothValue(x, z, seed);

                    // Nur oberhalb eines Schwellwerts entsteht eine Erzader
                    const float threshold = 0.55f;
                    _oreDeposits[x, z] = n > threshold ? (n - threshold) / (1f - threshold) * 500f : 0f;
                }
            }
        }

        public float GetOre(int x, int z) => IsInside(x, z) ? _oreDeposits[x, z] : 0f;

        // Entnimmt bis zu 'amount' Erz aus der Zelle, gibt zurück wie viel tatsächlich entnommen wurde
        public float ExtractOre(int x, int z, float amount, Miner miner)
        {
            if (!IsInside(x, z) || 
                amount <= 0f ||
                miner.getCapacityLeft() == 0f || 
                miner.getCapacityLeft() < amount) 
                return 0f;
            float taken = Math.Min(amount, _oreDeposits[x, z]);
            //Debug.WriteLine($"Extracting ore from cell ({x}, {z}): {taken}, Remaining: {_oreDeposits[x, z]}, Miner: {miner.OreBuffer}");
            _oreDeposits[x, z] -= taken;
            return taken;
        }

        public bool IsInside(int x, int z) => x >= 0 && x < Width && z >= 0 && z < Depth;
        public bool IsOccupied(int x, int z) => _cells.ContainsKey((x, z));

        public void Place(int x, int z, object obj) => _cells[(x, z)] = obj;
        public void Remove(int x, int z) => _cells.Remove((x, z));

        public object Get(int x, int z) => _cells.TryGetValue((x, z), out var o) ? o : null;

        // Weltkoordinate der Zellenmitte (Raster zentriert um den Ursprung)
        public Vector3 CellToWorld(int x, int z) =>
            new Vector3(
                (x - Width / 2f) * CellSize + CellSize / 2f,
                0f,
                (z - Depth / 2f) * CellSize + CellSize / 2f);

        // Wandelt eine Weltposition (Höhe y=0) in Rasterkoordinaten um
        public bool WorldToCell(Vector3 world, out int x, out int z)
        {
            x = (int)((world.X / CellSize) + Width / 2f);
            z = (int)((world.Z / CellSize) + Depth / 2f);
            return IsInside(x, z);
        }
    }
}
