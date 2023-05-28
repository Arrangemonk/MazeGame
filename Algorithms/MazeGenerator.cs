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
        Up = 16,
        Down = 32,
        Hozup = Horizontal + Up,
        Vertup = Vertical + Up,
        Hozdown = Horizontal + Down,
        Vertdown = Vertical + Down,
        Room = 64,
        RoomBlocked = 128,
        RoomBlockedHorizontal = RoomBlocked + Directions.East + Directions.West,
        RoomBlockedVertical = RoomBlocked + Directions.North + Directions.South,
        RoomBlockedCornerNorhEast = RoomBlocked + Directions.North + Directions.East,
        RoomBlockedCornerNorthWest = RoomBlocked + Directions.North + Directions.West,
        RoomBlockedCornderSouthEast = RoomBlocked + Directions.South + Directions.East,
        RoomBlockedCornerSouthWest = RoomBlocked + Directions.South + Directions.West,
        RoomBlockedTcrossHorizontalNorth = RoomBlocked + Directions.East + Directions.West + Directions.North,
        RoomBlockedTcrossHorizontalSouth = RoomBlocked + Directions.East + Directions.West + Directions.South,
        RoomBlockedTcrossVerticalEast = RoomBlocked + Directions.North + Directions.South + Directions.East,
        RoomBlockedTcrossVerticalWest = RoomBlocked + Directions.North + Directions.South + Directions.West,
        RoomBlockedEndNorth = RoomBlocked + Directions.North,
        RoomBlockedEndSouth = RoomBlocked + Directions.South,
        RoomBlockedEndEast = RoomBlocked + Directions.East,
        RoomBlockedEndWest = RoomBlocked + Directions.West,
        RoomBlockedCross = RoomBlocked + Directions.North + Directions.South + Directions.East + Directions.West,
    }

    public static class BlocksExtender
    {
        public static bool IsStraight(this Blocks block)
        {
            return block is Blocks.Horizontal or Blocks.Vertical;
        }
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

        private static HashSet<Rectangle> CarveRooms(ref Blocks[,] maze)
        {
            HashSet<Rectangle> rectangles = new();

            var maxamount = (int)(Math.Sqrt(Constants.Mazesize * 2));
            var amount = Rng.Next(maxamount, maxamount * 5);
            for (var i = 0; i < amount; i++)
            {
                var posx = Rng.Next(1, Constants.Mazesize - 1);
                var posy = Rng.Next(1, Constants.Mazesize - 1);
                var width = Rng.Next(Math.Min(2, maxamount / 2), Math.Max(2, maxamount / 2));
                var height = Rng.Next(Math.Min(2, maxamount / 2), Math.Max(2, maxamount / 2));


                var rect = new Rectangle(posx - 1, posy - 1, width + 1, height + 1);

                if (rectangles.Any(r => Raylib.CheckCollisionRecs(r, rect)))
                {
                    amount++;
                    continue;
                }

                rectangles.Add(rect);

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var carvex = Tools.Clamp(posx + x, Constants.Mazesize);
                        var carvey = Tools.Clamp(posy + y, Constants.Mazesize);

                        maze[carvex, carvey] = Blocks.Room;
                    }
                }
            }

            return rectangles;
        }

        private static void CarvePassagesFromIterative(int startX, int startY, ref Blocks[,] grid)
        {
            var path = new List<(int x, int y)>();
            var stack = new Stack<(int x, int y)>();
            //try to prefer straight lines a little
            var dirsstack = new Stack<Directions>();
            stack.Push((startX, startY));
            dirsstack.Push(Directions.South);
            path.Add((startX, startY));

            while (stack.Count > 0)
            {
                var (x, y) = stack.Peek();
                var olddir = dirsstack.Pop();
                var hasUnvisited = false;

                foreach (var dir in ValidDirections.Concat(new[] { olddir, olddir }).OrderBy(e => Rng.Next()))
                {
                    dirsstack.Push(dir);
                    var cx = Tools.Clamp(x + Dx(dir), Constants.Mazesize);
                    var cy = Tools.Clamp(y + Dy(dir), Constants.Mazesize);
                    if (!new[] { Blocks.Undefined, Blocks.Room }.Contains(grid[cx, cy]))
                        continue;

                    grid[x, y] = (Blocks)((int)dir + (int)grid[x, y]);
                    if (grid[cx, cy] == Blocks.Room)
                    {
                        var depth = 0;
                        var (rx, ry) = FillRoom(cx, cy, ref grid, ref depth);
                        if (depth > 1)
                            grid[rx, ry] = Blocks.Room;
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

        private static (int, int) FillRoom(int startX, int startY, ref Blocks[,] grid, ref int depth)
        {
            var filledPoints = new HashSet<(int, int)>();
            var stack = new Stack<(int, int)>();
            stack.Push((startX, startY));

            while (stack.Count > 0)
            {
                depth++;
                var (x, y) = stack.Pop();

                if (grid[x, y] != Blocks.Room)
                    continue;

                if ((x, y) != (startX, startY))
                    filledPoints.Add((x, y));
                grid[x, y] = Blocks.RoomBlocked;

                // Check all 8 neighboring cells
                for (var dx = -1; dx <= 1; dx++)
                {
                    for (var dy = -1; dy <= 1; dy++)
                    {
                        var cx = Tools.Clamp(x + dx, Constants.Mazesize);
                        var cy = Tools.Clamp(y + dy, Constants.Mazesize);

                        if (grid[cx, cy] == Blocks.Room)
                        {
                            stack.Push((cx, cy));
                        }
                    }
                }
            }
            var borderPoints = GetBorderPoints(ref filledPoints);
            var count = borderPoints.Count;
            return count == 0 ? (startX, startY) : borderPoints[Rng.Next(borderPoints.Count)];
        }

        private static List<(int, int)> GetBorderPoints(ref HashSet<(int, int)> filledPoints)
        {
            var borderPoints = new List<(int, int)>();

            foreach (var (x, y) in filledPoints)
            {
                if (IsOnBorder(x, y, ref filledPoints))
                {
                    borderPoints.Add((x, y));
                }
            }

            return borderPoints;
        }

        private static bool IsOnBorder(int x, int y, ref HashSet<(int, int)> filledPoints)
        {
            return !filledPoints.Contains(Tools.Clamp((x + 1, y), Constants.Mazesize)) || !filledPoints.Contains(Tools.Clamp((x - 1, y), Constants.Mazesize)) ||
                   !filledPoints.Contains(Tools.Clamp((x, y + 1), Constants.Mazesize)) || !filledPoints.Contains(Tools.Clamp((x, y - 1), Constants.Mazesize));
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

        public static Dictionary<Blocks, Model> PrepareMazeParts(Shader shader, string basepath,
            ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref List<Model> models)
        {
            var rot000 = Matrix4x4.Identity;
            var rot090 = Matrix4x4.CreateRotationY(Tools.Pi * .5f);
            var rot180 = Matrix4x4.CreateRotationY(Tools.Pi);
            var rot270 = Matrix4x4.CreateRotationY(Tools.Pi * -.5f);
            const string straight = nameof(straight);
            const string corner = nameof(corner);
            const string tcross = nameof(tcross);
            const string end = nameof(end);
            const string cross = nameof(cross);
            const string room = nameof(room);

            var result = new Dictionary<Blocks, Model>
            {
                { Blocks.Horizontal, Tools.PrepareModel(Path.Combine(basepath,straight), basepath, shader, rot090, ref textures,ref models) },
                { Blocks.Vertical, Tools.PrepareModel(Path.Combine(basepath,straight), basepath, shader, rot000, ref textures, ref models) },
                { Blocks.CornerNorhEast, Tools.PrepareModel(Path.Combine(basepath,corner), basepath, shader, rot180, ref textures, ref models) },
                { Blocks.CornerNorthWest, Tools.PrepareModel(Path.Combine(basepath,corner), basepath, shader, rot270, ref textures, ref models) },
                { Blocks.CornderSouthEast, Tools.PrepareModel(Path.Combine(basepath,corner), basepath, shader, rot090, ref textures, ref models) },
                { Blocks.CornerSouthWest, Tools.PrepareModel(Path.Combine(basepath,corner), basepath, shader, rot000, ref textures, ref models) },
                { Blocks.TcrossHorizontalNorth, Tools.PrepareModel(Path.Combine(basepath,tcross), basepath, shader, rot180, ref textures, ref models) },
                { Blocks.TcrossHorizontalSouth, Tools.PrepareModel(Path.Combine(basepath,tcross), basepath, shader, rot000, ref textures, ref models) },
                { Blocks.TcrossVerticalEast, Tools.PrepareModel(Path.Combine(basepath,tcross), basepath, shader, rot090, ref textures, ref models) },
                { Blocks.TcrossVerticalWest, Tools.PrepareModel(Path.Combine(basepath,tcross), basepath, shader, rot270, ref textures, ref models) },
                { Blocks.EndNorth, Tools.PrepareModel(Path.Combine(basepath,end), basepath, shader, rot180, ref textures, ref models) },
                { Blocks.EndSouth, Tools.PrepareModel(Path.Combine(basepath,end), basepath, shader, rot000, ref textures, ref models) },
                { Blocks.EndEast, Tools.PrepareModel(Path.Combine(basepath,end), basepath, shader, rot090, ref textures, ref models) },
                { Blocks.EndWest, Tools.PrepareModel(Path.Combine(basepath,end), basepath, shader, rot270, ref textures, ref models) },
                { Blocks.Cross, Tools.PrepareModel(Path.Combine(basepath,cross), basepath, shader, rot180, ref textures, ref models) },
                { Blocks.Undefined,  Tools.PrepareModel(Path.Combine(basepath,room), basepath, shader, rot180, ref textures, ref models) },
                { Blocks.Room,  Tools.PrepareModel(Path.Combine(basepath,room), basepath, shader, rot000, ref textures, ref models) },
                { Blocks.RoomBlocked,  Tools.PrepareModel(Path.Combine(basepath,room), basepath, shader, rot000, ref textures, ref models)}
            };
            return result;
        }

        public static Dictionary<Blocks, Model> PrepareUpwardsParts(Shader shader,
    ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref List<Model> models)
        {
            var rot000 = Matrix4x4.Identity;
            var rot090 = Matrix4x4.CreateRotationY(Tools.Pi * .5f);
            var rot180 = Matrix4x4.CreateRotationY(Tools.Pi);
            var rot270 = Matrix4x4.CreateRotationY(Tools.Pi * -.5f);
            const string upwards_pipe = nameof(upwards_pipe);
            const string pipe = nameof(pipe);

            var result = new Dictionary<Blocks, Model>
            {
                { Blocks.Hozup, Tools.PrepareModel(upwards_pipe, pipe, shader, rot090, ref textures,ref models) },
                { Blocks.Vertup, Tools.PrepareModel(upwards_pipe, pipe, shader, rot000, ref textures, ref models) },
                { Blocks.Hozdown, Tools.PrepareModel(upwards_pipe, pipe, shader, rot270, ref textures,ref models) },
                { Blocks.Vertdown, Tools.PrepareModel(upwards_pipe, pipe, shader, rot180, ref textures, ref models) },
            };
            return result;
        }

        public static Dictionary<Blocks, Model> PrepareStairsParts(Shader shader,
            ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref List<Model> models)
        {
            var rot000 = Matrix4x4.Identity;
            var rot090 = Matrix4x4.CreateRotationY(Tools.Pi * .5f);
            var rot180 = Matrix4x4.CreateRotationY(Tools.Pi);
            var rot270 = Matrix4x4.CreateRotationY(Tools.Pi * -.5f);
            const string upwards_stairs = nameof(upwards_stairs);
            const string plaster = nameof(plaster);

            var result = new Dictionary<Blocks, Model>
            {
                { Blocks.Hozup, Tools.PrepareModel(upwards_stairs, plaster, shader, rot090, ref textures,ref models) },
                { Blocks.Vertup, Tools.PrepareModel(upwards_stairs, plaster, shader, rot000, ref textures, ref models) },
                { Blocks.Hozdown, Tools.PrepareModel(upwards_stairs, plaster, shader, rot270, ref textures,ref models) },
                { Blocks.Vertdown, Tools.PrepareModel(upwards_stairs, plaster, shader, rot180, ref textures, ref models) },
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

        public static Dictionary<Blocks, Rectangle[]> PrepareMazeCollosion()
        {

            //const float plow = -0.2f;
            //const float phigh = 0.8f;

            //const float wlow = 0.4f;
            //const float whigh = 1.4f;

            const float plow = 0.0f;
            const float phigh = 0.8f;

            const float wlow = 0.2f;
            const float whigh = 1.0f;

            var northeast = new Rectangle(plow, plow, wlow, wlow);
            var northwest = new Rectangle(phigh, plow, wlow, wlow);
            var southeast = new Rectangle(plow, phigh, wlow, wlow);
            var southwest = new Rectangle(phigh, phigh, wlow, wlow);

            var north = new Rectangle(plow, plow, whigh, wlow);
            var south = new Rectangle(plow, phigh, whigh, wlow);
            var east = new Rectangle(plow, plow, wlow, whigh);
            var west = new Rectangle(phigh, plow, wlow, whigh);



            var result = new Dictionary<Blocks, Rectangle[]>
            {
                { Blocks.Horizontal,                        new[]{north,south}},
                { Blocks.Vertical,                          new[]{east,west}},
                { Blocks.CornerNorhEast,                    new[]{northeast,west,south}},
                { Blocks.CornerNorthWest,                   new[]{northwest,east,south}},
                { Blocks.CornderSouthEast,                  new[]{southeast,north,west}},
                { Blocks.CornerSouthWest,                   new[]{southwest,north,east}},
                { Blocks.TcrossHorizontalNorth,             new[]{northeast,northwest,south,}},
                { Blocks.TcrossHorizontalSouth,             new[]{southeast,southwest,north}},
                { Blocks.TcrossVerticalEast,                new[]{northeast,southeast, west}},
                { Blocks.TcrossVerticalWest,                new[]{northwest,southwest,east}},
                { Blocks.EndNorth,                          new[]{east,south,west}},
                { Blocks.EndSouth,                          new[]{east,north,west}},
                { Blocks.EndEast,                           new[]{north,west,south}},
                { Blocks.EndWest,                           new[]{north,east,south}},
                { Blocks.Cross,                             new[]{northeast,northwest,southeast,southwest}},
                { Blocks.RoomBlockedHorizontal,             new[]{north,south}},
                { Blocks.RoomBlockedVertical,               new[]{east,west}},
                { Blocks.RoomBlockedCornerNorhEast,         new[]{west,south}},
                { Blocks.RoomBlockedCornerNorthWest,        new[]{east,south}},
                { Blocks.RoomBlockedCornderSouthEast,       new[]{north,west}},
                { Blocks.RoomBlockedCornerSouthWest,        new[]{north,east}},
                { Blocks.RoomBlockedTcrossHorizontalNorth,  new[]{south,}},
                { Blocks.RoomBlockedTcrossHorizontalSouth,  new[]{north}},
                { Blocks.RoomBlockedTcrossVerticalEast,     new[]{west}},
                { Blocks.RoomBlockedTcrossVerticalWest,     new[]{east}},
                { Blocks.RoomBlockedEndNorth,               new[]{east,south,west}},
                { Blocks.RoomBlockedEndSouth,               new[]{east,north,west}},
                { Blocks.RoomBlockedEndEast,                new[]{north,west,south}},
                { Blocks.RoomBlockedEndWest,                new[]{north,east,south}},
                { Blocks.RoomBlockedCross,                  Array.Empty<Rectangle>()},
                { Blocks.Undefined,                         Array.Empty<Rectangle>()},
                { Blocks.Room,                              Array.Empty<Rectangle>()},
                { Blocks.RoomBlocked,                       Array.Empty<Rectangle>()},
            };
            return result;
        }

        public static Rectangle Mazerect(int x, int y, int size) => new(x * size, y * size, size, size);
    }
}