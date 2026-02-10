using System.Numerics;
using Raylib_CSharp.Colors;

namespace MazeGame.Common;

internal static class Constants
{
    public const int Mazesize = 225;
    public const int Exitpos = Constants.Mazesize - 1;
    public const float Scale = 1.0f / 30;
    public static readonly Color Tint = Color.White;
    public static readonly Color ClsColor = Color.FromNormalized(new Vector4(0.08f, 0.1f, 0.055f, 1.0f));
    public const int Blocksize = 16;
    public const float Ticks = 60;
    public static readonly Vector3 DefaultOffset = new(0.5f, 0, 0.5f);
    public static readonly Vector3 Maxcam = new(Constants.Mazesize, 1, Constants.Mazesize);
    public static float TOLERANCE = 0.1f;
}

public enum GameState
{
    Starting,
    Menu,
    Game,
    Credits
}