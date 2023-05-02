using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static MazeGame.Common.Tools;

namespace MazeGame.Algorithms
{
    public enum Directions
    {
        North = 1,
        South = 2,
        East = 4,
        West = 8
    }
    public enum Blocks
    {
        Undefined = 0,
        Horizontal = Directions.East + Directions.West,
        Vertical = Directions.North + Directions.South,
        CornerNorhEast = Directions.North + Directions.East,
        CornerNorthWest = Directions.North + Directions.West,
        CornderSouthEast = Directions.South + Directions.East,
        CornerSouthWest = Directions.South + Directions.West,
        TcrossHorizontalNorth = Directions.East + Directions.West + Directions.North,
        TcrossHorizontalSouth = Directions.East + Directions.West + Directions.South,
        TcrossVerticalEast = Directions.North + Directions.South + Directions.East,
        TcrossVerticalWest = Directions.North + Directions.South + Directions.West,
        EndNorth = Directions.North,
        EndSouth = Directions.South,
        EndEast = Directions.East,
        EndWest = Directions.West,
        Cross = Directions.North + Directions.South + Directions.East + Directions.West
    }



    class MazeGenerator
    {
        private static readonly Random Rng = new Random();

        public static Blocks[,] GenerateMaze(int rows, int cols)
        {
            var maze = new Blocks[rows, cols];

            CarvePassagesFrom(Rng.Next(0, rows - 1), Rng.Next(0, cols - 1), maze);
            return maze;
        }

        public static void PrintMaze(Blocks[,] maze)
        {
            for (var y = 0; y < maze.GetLength(0); y++)
            {
                for (var x = 0; x < maze.GetLength(1); x++)
                {
                    switch (maze[x, y])
                    {
                        case Blocks.Horizontal:
                            Console.Write("─");
                            break;
                        case Blocks.Vertical:
                            Console.Write("│");
                            break;
                        case Blocks.CornerNorhEast:
                            Console.Write("┘");
                            break;
                        case Blocks.CornerNorthWest:
                            Console.Write("└");
                            break;
                        case Blocks.CornderSouthEast:
                            Console.Write("┐");
                            break;
                        case Blocks.CornerSouthWest:
                            Console.Write("┌");
                            break;
                        case Blocks.TcrossHorizontalNorth:
                            Console.Write("┴");
                            break;
                        case Blocks.TcrossHorizontalSouth:
                            Console.Write("┬");
                            break;
                        case Blocks.TcrossVerticalEast:
                            Console.Write("┤");
                            break;
                        case Blocks.TcrossVerticalWest:
                            Console.Write("├");
                            break;
                        case Blocks.EndNorth:
                            Console.Write("x");
                            break;
                        case Blocks.EndSouth:
                            Console.Write("x");
                            break;
                        case Blocks.EndEast:
                            Console.Write("x");
                            break;
                        case Blocks.EndWest:
                            Console.Write("x");
                            break;
                        case Blocks.Cross:
                            Console.Write("┼");
                            break;
                    }
                }
                Console.WriteLine();
            }
        }

        private static void CarvePassagesFrom(int x, int y, Blocks[,] grid)
        {
            var directions = new List<Directions> { Directions.North, Directions.South, Directions.East, Directions.West }.Shuffle(Rng);

            foreach (var dir in directions)
            {
                var cx = x + Dx(dir);
                var cy = y + Dy(dir);
                if (0 <= cx && cx < grid.GetLength(0)
                 && 0 <= cy && cy < grid.GetLength(1)
                 && grid[cx, cy] == Blocks.Undefined)
                {
                    grid[x, y] = (Blocks)((int)dir + (int)grid[x, y]);
                    grid[cx, cy] = (Blocks)Opposite(dir);
                    CarvePassagesFrom(cx, cy, grid);
                }
            }

        }

        private static int Dx(Directions dir)
        {
            return dir switch
            {
                Directions.North => 0,
                Directions.South => 0,
                Directions.East => -1,
                Directions.West => 1
            };
        }

        private static int Dy(Directions dir)
        {
            return dir switch
            {
                Directions.North => -1,
                Directions.South => 1,
                Directions.East => 0,
                Directions.West => 0
            };
        }

        private static Directions Opposite(Directions dir)
        {
            return dir switch
            {
                Directions.North => Directions.South,
                Directions.South => Directions.North,
                Directions.East => Directions.West,
                Directions.West => Directions.East
            };
        }

        public static Dictionary<Blocks, Model> PrepareMazeParts(Shader shader, ref Dictionary<string, Dictionary<string, Texture2D>> textures)
        {
            var rot000 = Matrix4x4.Identity;
            var rot090 = Matrix4x4.CreateRotationY(PI * .5f);
            var rot180 = Matrix4x4.CreateRotationY(PI);
            var rot270 = Matrix4x4.CreateRotationY(PI * -.5f);

            var result = new Dictionary<Blocks, Model>
            {
                { Blocks.Horizontal, PrepareModel("straight", "pipe", shader,rot090, ref textures) },
                { Blocks.Vertical, PrepareModel("straight", "pipe", shader,rot000, ref textures) },
                { Blocks.CornerNorhEast, PrepareModel("corner", "pipe", shader,rot000, ref textures) },
                { Blocks.CornerNorthWest, PrepareModel("corner", "pipe", shader,rot090, ref textures) },
                { Blocks.CornderSouthEast, PrepareModel("corner", "pipe", shader,rot270, ref textures) },
                { Blocks.CornerSouthWest, PrepareModel("corner", "pipe", shader,rot180, ref textures) },
                { Blocks.TcrossHorizontalNorth, PrepareModel("tcross", "pipe", shader,rot000, ref textures) },
                { Blocks.TcrossHorizontalSouth, PrepareModel("tcross", "pipe", shader,rot180, ref textures) },
                { Blocks.TcrossVerticalEast, PrepareModel("tcross", "pipe", shader,rot270, ref textures) },
                { Blocks.TcrossVerticalWest, PrepareModel("tcross", "pipe", shader,rot090, ref textures) },
                { Blocks.EndNorth, PrepareModel("end", "pipe", shader,rot000, ref textures) },
                { Blocks.EndSouth, PrepareModel("end", "pipe", shader,rot180, ref textures) },
                { Blocks.EndEast, PrepareModel("end", "pipe", shader,rot270, ref textures) },
                { Blocks.EndWest, PrepareModel("end", "pipe", shader,rot090, ref textures) },
                { Blocks.Cross, PrepareModel("cross", "pipe", shader,rot000, ref textures) }
            };
            return result;
        }
    }

    public static class ShuffleExtensions
    {

        public static List<T> Shuffle<T>(this List<T> list, Random rng)
        {
            for (var n = list.Count - 1; n >= 0; n--)
            {
                var k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }
    }
}