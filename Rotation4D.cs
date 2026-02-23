using OpenTK.Mathematics;
using System;

namespace Tapatakt4D;

/// <summary>
/// 4D rotation matrix generator.
/// </summary>
public static class Rotation4D
{
    /// <summary>
    /// Creates a rotation matrix for rotation in the XY plane.
    /// </summary>
    public static Matrix4 CreateRotationXY(float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        return new Matrix4(
            c, -s, 0, 0,
            s, c, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );
    }

    /// <summary>
    /// Creates a rotation matrix for rotation in the XZ plane.
    /// </summary>
    public static Matrix4 CreateRotationXZ(float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        return new Matrix4(
            c, 0, -s, 0,
            0, 1, 0, 0,
            s, 0, c, 0,
            0, 0, 0, 1
        );
    }

    /// <summary>
    /// Creates a rotation matrix for rotation in the XW plane.
    /// </summary>
    public static Matrix4 CreateRotationXW(float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        return new Matrix4(
            c, 0, 0, -s,
            0, 1, 0, 0,
            0, 0, 1, 0,
            s, 0, 0, c
        );
    }

    /// <summary>
    /// Creates a rotation matrix for rotation in the YZ plane.
    /// </summary>
    public static Matrix4 CreateRotationYZ(float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        return new Matrix4(
            1, 0, 0, 0,
            0, c, -s, 0,
            0, s, c, 0,
            0, 0, 0, 1
        );
    }

    /// <summary>
    /// Creates a rotation matrix for rotation in the YW plane.
    /// </summary>
    public static Matrix4 CreateRotationYW(float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        return new Matrix4(
            1, 0, 0, 0,
            0, c, 0, -s,
            0, 0, 1, 0,
            0, s, 0, c
        );
    }

    /// <summary>
    /// Creates a rotation matrix for rotation in the ZW plane.
    /// </summary>
    public static Matrix4 CreateRotationZW(float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        return new Matrix4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, c, -s,
            0, 0, s, c
        );
    }
}
