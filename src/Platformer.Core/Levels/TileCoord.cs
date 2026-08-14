namespace Platformer.Core.Levels;

/// <summary>
/// Integer position of a cell in a <see cref="TileGrid"/>, with the origin at
/// the top-left tile and Y increasing downwards, matching the screen-space
/// convention the renderer and the ASCII level format both use.
/// </summary>
/// <param name="X">Column index; may be negative, which is simply out of bounds.</param>
/// <param name="Y">Row index; may be negative, which is simply out of bounds.</param>
public readonly record struct TileCoord(int X, int Y);
