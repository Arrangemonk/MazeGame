using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MazeGame.Common;
using Raylib_cs;

namespace MazeGame.Algorithms
{
    public enum Directions
    {
        Undefined = 0,
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
        Cross = Directions.North + Directions.South + Directions.East + Directions.West,
        Room = 16,
        RoomBlocked = 32,
    }


    class MazeGenerator
    {
        public static IEnumerable<Directions> ValidDirections = Enum.GetValues(typeof(Directions)).Cast<Directions>()
            .Where(dir => dir != Directions.Undefined).ToList();


        public static readonly Random Rng = new();

        public static int[,] GenerateRandomIntegers(int rows, int cols)
        {
            var result = new int[rows, cols];
            for (var x = 0; x < cols; x++)
                for (var y = 0; y < rows; y++)
                    result[x, y] = Rng.Next(0, 100);
            return result;
        }

        public static Blocks[,] GenerateMaze(int rows, int cols)
        {
            var maze = new Blocks[rows, cols];

            CarveRooms(ref maze);
            var x = Rng.Next(0, rows - 1);
            var y = Rng.Next(0, cols - 1);

            while (maze[x, y] != Blocks.Undefined)
            {
                x = Rng.Next(0, rows - 1);
                y = Rng.Next(0, cols - 1);
            }

            CarvePassagesFromIterative(x, y, ref maze);
            return maze;
        }

        private static void CarveRooms(ref Blocks[,] maze)
        {
            var mazewidth = GameLoop.Mazesize;
            var mazeheight = GameLoop.Mazesize;

            var maxamount = (int)(Math.Sqrt(mazewidth + mazeheight));
            var amount = Rng.Next(maxamount, maxamount * 5);
            for (var i = 0; i < amount; i++)
            {
                var posx = Rng.Next(1, mazewidth - 1);
                var posy = Rng.Next(1, mazeheight - 1);
                var width = Rng.Next(Math.Min(2, maxamount / 2), Math.Max(2, maxamount / 2));
                var height = Rng.Next(Math.Min(2, maxamount / 2), Math.Max(2, maxamount / 2));
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var carvex = Tools.Clamp(posx + x, mazewidth);
                        var carvey = Tools.Clamp(posy + y, mazeheight);

                        maze[carvex, carvey] = Blocks.Room;
                    }
                }
            }
        }


        private static void CarvePassagesFrom(int x, int y, ref Blocks[,] grid)
        {
            var directions = ValidDirections.OrderBy(e => Rng.Next());

            foreach (var dir in directions)
            {
                var cx = x + Dx(dir);
                var cy = y + Dy(dir);
                if (0 > cx || cx >= GameLoop.Mazesize
                           || 0 > cy || cy >= GameLoop.Mazesize
                           || grid[cx, cy] != Blocks.Undefined)
                    continue;
                grid[x, y] = (Blocks)((int)dir + (int)grid[x, y]);
                grid[cx, cy] = (Blocks)Opposite(dir);
                CarvePassagesFrom(cx, cy, ref grid);
            }
        }

        private static void CarvePassagesFromIterative(int startX, int startY, ref Blocks[,] grid)
        {
            var path = new List<(int x, int y)>();
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            path.Add((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Peek();
                var directions = ValidDirections.OrderBy(e => Rng.Next()).ToList();
                bool hasUnvisited = false;

                foreach (var dir in directions)
                {
                    var cx = Tools.Clamp(x + Dx(dir), GameLoop.Mazesize);
                    var cy = Tools.Clamp(y + Dy(dir), GameLoop.Mazesize);
                    if (
                        //0 > cx || cx >= GameLoop.Mazesize ||
                        //0 > cy || cy >= GameLoop.Mazesize ||
                        !new[] { Blocks.Undefined, Blocks.Room }.Contains(grid[cx, cy]))
                        continue;

                    grid[x, y] = (Blocks)((int)dir + (int)grid[x, y]);
                    if (grid[cx, cy] == Blocks.Room)
                    {
                        grid[cx, cy] = Blocks.RoomBlocked;
                        FillRoom(cx, cy, ref grid, 0);
                        continue;
                    }
                    grid[cx, cy] = (Blocks)Opposite(dir);
                    stack.Push((cx, cy));
                    path.Add((cx, cy));
                    hasUnvisited = true;
                    break;
                }

                if (hasUnvisited) continue;
                stack.Pop();
                if (path.Count <= 1) continue;
                path.RemoveAt(path.Count - 1);
                var prev = path[^1];
                stack.Push(prev);
            }
        }

        private static void FillRoom(int x, int y, ref Blocks[,] grid, int depth)
        {
            if (depth > 2)
                return;
            depth++;
            foreach (var dir in ValidDirections)
            {
                var cx = Tools.Clamp(x + Dx(dir), GameLoop.Mazesize);
                var cy = Tools.Clamp(y + Dy(dir), GameLoop.Mazesize);
                if (
                //0 > cx || cx >= GameLoop.Mazesize
                //       || 0 > cy || cy >= GameLoop.Mazesize
                //    || 
                grid[cx, cy] != Blocks.Room)
                    continue;
                grid[cx, cy] = Blocks.RoomBlocked;
                FillRoom(cx, cy, ref grid, depth);
            }
        }

        public static IEnumerable<Directions> Dirx(float x)
        {
            x = (float)Math.Round(x * 2, MidpointRounding.AwayFromZero);
            return x < 0 ? new[] { Directions.East } :
                x > 0 ? new[] { Directions.West } : new[] { Directions.East, Directions.West };
        }

        public static IEnumerable<Directions> Diry(float y)
        {
            y = (float)Math.Round(y * 2, MidpointRounding.AwayFromZero);
            return y < 0 ? new[] { Directions.North } :
                y > 0 ? new[] { Directions.South } : new[] { Directions.North, Directions.South };
        }


        public static int Dx(Directions dir)
        {
            return dir switch
            {
                Directions.North => 0,
                Directions.South => 0,
                Directions.East => -1,
                Directions.West => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
            };
        }

        public static int Dy(Directions dir)
        {
            return dir switch
            {
                Directions.North => -1,
                Directions.South => 1,
                Directions.East => 0,
                Directions.West => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
            };
        }

        public static IEnumerable<Directions> DirectionsFromblock(Blocks block)
        {
            return ValidDirections
                .Where(direction => block >= Blocks.Room || ((int)block & (int)direction) != 0);
        }

        public static Directions Opposite(Directions dir)
        {
            return dir switch
            {
                Directions.North => Directions.South,
                Directions.South => Directions.North,
                Directions.East => Directions.West,
                Directions.West => Directions.East,
                _ => Directions.Undefined,
            };
        }

        public static Dictionary<Blocks, Model> PrepareMazeParts(Shader shader,
            ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref List<Model> models)
        {
            var rot000 = Matrix4x4.Identity;
            var rot090 = Matrix4x4.CreateRotationY(Tools.Pi * .5f);
            var rot180 = Matrix4x4.CreateRotationY(Tools.Pi);
            var rot270 = Matrix4x4.CreateRotationY(Tools.Pi * -.5f);
            const string straight = nameof(straight);
            const string pipe = nameof(pipe);
            const string corner = nameof(corner);
            const string tcross = nameof(tcross);
            const string end = nameof(end);
            const string cross = nameof(cross);
            const string room = nameof(room);

            var result = new Dictionary<Blocks, Model>
            {
                { Blocks.Horizontal, Tools.PrepareModel(straight, pipe, shader, rot090, ref textures,ref models) },
                { Blocks.Vertical, Tools.PrepareModel(straight, pipe, shader, rot000, ref textures, ref models) },
                { Blocks.CornerNorhEast, Tools.PrepareModel(corner, pipe, shader, rot180, ref textures, ref models) },
                { Blocks.CornerNorthWest, Tools.PrepareModel(corner, pipe, shader, rot270, ref textures, ref models) },
                { Blocks.CornderSouthEast, Tools.PrepareModel(corner, pipe, shader, rot090, ref textures, ref models) },
                { Blocks.CornerSouthWest, Tools.PrepareModel(corner, pipe, shader, rot000, ref textures, ref models) },
                { Blocks.TcrossHorizontalNorth, Tools.PrepareModel(tcross, pipe, shader, rot180, ref textures, ref models) },
                { Blocks.TcrossHorizontalSouth, Tools.PrepareModel(tcross, pipe, shader, rot000, ref textures, ref models) },
                { Blocks.TcrossVerticalEast, Tools.PrepareModel(tcross, pipe, shader, rot090, ref textures, ref models) },
                { Blocks.TcrossVerticalWest, Tools.PrepareModel(tcross, pipe, shader, rot270, ref textures, ref models) },
                { Blocks.EndNorth, Tools.PrepareModel(end, pipe, shader, rot180, ref textures, ref models) },
                { Blocks.EndSouth, Tools.PrepareModel(end, pipe, shader, rot000, ref textures, ref models) },
                { Blocks.EndEast, Tools.PrepareModel(end, pipe, shader, rot090, ref textures, ref models) },
                { Blocks.EndWest, Tools.PrepareModel(end, pipe, shader, rot270, ref textures, ref models) },
                { Blocks.Cross, Tools.PrepareModel(cross, pipe, shader, rot180, ref textures, ref models) },
                { Blocks.Undefined,  Tools.PrepareModel(room, pipe, shader, rot180, ref textures, ref models) },
                { Blocks.Room,  Tools.PrepareModel(room, pipe, shader, rot000, ref textures, ref models) },
                { Blocks.RoomBlocked,  Tools.PrepareModel(room, pipe, shader, rot000, ref textures, ref models)}
            };
            return result;
        }

        public static Dictionary<Blocks, Rectangle> PrepareMazePrint(int size)
        {
            var result = new Dictionary<Blocks, Rectangle>
            {
                { Blocks.Horizontal,           Mazerect( 2, 0 ,size) },
                { Blocks.Vertical,             Mazerect( 1, 0 ,size) },
                { Blocks.CornerNorhEast,       Mazerect( 3, 0 ,size) },
                { Blocks.CornerNorthWest,      Mazerect( 1, 0 ,size) },
                { Blocks.CornderSouthEast,     Mazerect( 2, 0 ,size) },
                { Blocks.CornerSouthWest,      Mazerect( 0, 0 ,size) },
                { Blocks.TcrossHorizontalNorth,Mazerect( 3, 0 ,size) },
                { Blocks.TcrossHorizontalSouth,Mazerect( 2, 0 ,size) },
                { Blocks.TcrossVerticalEast,   Mazerect( 3, 0 ,size) },
                { Blocks.TcrossVerticalWest,   Mazerect( 1, 0 ,size) },
                { Blocks.EndNorth,             Mazerect( 1, 0 ,size) },
                { Blocks.EndSouth,             Mazerect( 0, 0 ,size) },
                { Blocks.EndEast,              Mazerect( 2, 0 ,size) },
                { Blocks.EndWest,              Mazerect( 0, 0 ,size) },
                { Blocks.Cross,                Mazerect( 3, 0 ,size) },
                { Blocks.Undefined,            Mazerect( 0, 0 ,size) },
                { Blocks.Room,                 Mazerect( 0, 0 ,size) },
                { Blocks.RoomBlocked,          Mazerect( 0, 0 ,size) }
            };
            return result;
        }

        private static Rectangle Mazerect(int x, int y, int size) => new Rectangle(x * size, y * size, size, size);
    }
}