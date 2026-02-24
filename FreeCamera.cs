using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace Tapatakt4D;

/// <summary>
/// Free 4D camera using dual quaternion representation with full rotation freedom.
/// Any 4D rotation can be represented as v' = left * v * right
/// where left and right are unit quaternions.
/// </summary>
public class FreeCamera : ICamera
{
    /// <summary>
    /// Camera position in 4D world space.
    /// </summary>
    public Vector4 Position { get; set; }

    /// <summary>
    /// Left rotation quaternion.
    /// </summary>
    /// <summary>
    /// Left rotation quaternion.
    /// </summary>
    protected Quaternion _left = Quaternion.Identity;

    /// <summary>
    /// Right rotation quaternion.
    /// </summary>
    protected Quaternion _right = Quaternion.Identity;

    /// <summary>
    /// Gets the left quaternion for world-to-camera transform.
    /// </summary>
    public Quaternion LeftQuaternion => _left;

    /// <summary>
    /// Gets the right quaternion for world-to-camera transform.
    /// </summary>
    public Quaternion RightQuaternion => _right;

    /// <summary>
    /// Gets the inverse left quaternion for world-to-camera transform.
    /// </summary>
    public Quaternion LeftQuaternionInverse
        => Quaternion.Conjugate(_left); // for unit quaternions invertion and conjugation are the same

    /// <summary>
    /// Gets the inverse right quaternion for world-to-camera transform.
    /// </summary>
    public Quaternion RightQuaternionInverse
        => Quaternion.Conjugate(_right); // for unit quaternions invertion and conjugation are the same

    /// <summary>
    /// Projection distance for perspective.
    /// </summary>
    public float ProjectionDistance { get; set; }

    /// <summary>
    /// Creates a new free camera at the specified position.
    /// </summary>
    public FreeCamera(Vector4? position = null, float projectionDistance = 2.0f)
    {
        Position = position ?? new Vector4(0.0f, 0.0f, 8.0f, 0.0f);
        ProjectionDistance = projectionDistance;
    }

    /// <summary>
    /// Moves the camera by the specified delta relative to camera orientation.
    /// </summary>
    public void Move(Vector4 delta)
    {
        // Transform camera-space delta to world space
        Quaternion deltaQ = new(delta.Xyz, delta.W);
        Quaternion worldDeltaQ = _left * deltaQ * Quaternion.Conjugate(_right);
        Vector4 worldDelta = new(worldDeltaQ.Xyz, worldDeltaQ.W);
        Position += worldDelta;
    }

    /// <summary>
    /// Rotates the camera by the specified angles in the given planes.
    /// </summary>
    public void Rotate(float angleXY = 0, float angleXZ = 0, float angleXW = 0,
                        float angleYZ = 0, float angleYW = 0, float angleZW = 0)
    {
        if (angleXY != 0) RotateXY(angleXY);
        if (angleXZ != 0) RotateXZ(angleXZ);
        if (angleYZ != 0) RotateYZ(angleYZ);
        if (angleXW != 0) RotateXW(angleXW);
        if (angleYW != 0) RotateYW(angleYW);
        if (angleZW != 0) RotateZW(angleZW);
    }

    /// <summary>
    /// Rotates in the XY plane (roll).
    /// </summary>
    public void RotateXY(float angle)
    {
        (float c, float s) = CosSin(angle);
        Quaternion q = new(0, 0, s, c);
        Apply(q, q);
    }

    /// <summary>
    /// Rotates in the XZ plane (pitch).
    /// </summary>
    public void RotateXZ(float angle)
    {
        (float c, float s) = CosSin(angle);
        Quaternion q = new(0, s, 0, c);
        Apply(q, q);
    }

    /// <summary>
    /// Rotates in the YZ plane (yaw).
    /// </summary>
    public void RotateYZ(float angle)
    {
        (float c, float s) = CosSin(angle);
        Quaternion q = new(s, 0, 0, c);
        Apply(q, q);
    }

    /// <summary>
    /// Rotates in the XW plane.
    /// </summary>
    public void RotateXW(float angle)
    {
        (float c, float s) = CosSin(angle);
        Quaternion l = new(s, 0, 0, c);
        Quaternion r = new(-s, 0, 0, c);
        Apply(l, r);
    }

    /// <summary>
    /// Rotates in the YW plane.
    /// </summary>
    public void RotateYW(float angle)
    {
        (float c, float s) = CosSin(angle);
        Quaternion l = new(0, s, 0, c);
        Quaternion r = new(0, -s, 0, c);
        Apply(l, r);
    }

    /// <summary>
    /// Rotates in the ZW plane.
    /// </summary>
    public void RotateZW(float angle)
    {
        (float c, float s) = CosSin(angle);
        Quaternion l = new(0, 0, s, c);
        Quaternion r = new(0, 0, -s, c);
        Apply(l, r);
    }

    /// <summary>
    /// Computes cos(half) and sin(half) for the given angle.
    /// </summary>
    private static (float c, float s) CosSin(float angle)
    {
        float half = angle * 0.5f;
        return (MathF.Cos(half), MathF.Sin(half));
    }

    /// <summary>
    /// Applies left and right quaternions to the current rotation with normalization.
    /// </summary>
    private void Apply(Quaternion left, Quaternion right)
    {
        _left = Quaternion.Normalize(left * _left);
        _right = Quaternion.Normalize(_right * right);
    }

    /// <summary>
    /// Gets the forward direction vector (-Z in camera space).
    /// </summary>
    public Vector4 Forward => RotateVector(new Vector4(0, 0, -1, 0));

    /// <summary>
    /// Gets the right direction vector (+X in camera space).
    /// </summary>
    public Vector4 Right => RotateVector(new Vector4(1, 0, 0, 0));

    /// <summary>
    /// Gets the up direction vector (+Y in camera space).
    /// </summary>
    public Vector4 Up => RotateVector(new Vector4(0, 1, 0, 0));

    /// <summary>
    /// Gets the ana direction vector (+W in camera space).
    /// </summary>
    public Vector4 Ana => RotateVector(new Vector4(0, 0, 0, 1));

    private Vector4 RotateVector(Vector4 v)
    {
        Quaternion q = new(v.X, v.Y, v.Z, v.W);
        Quaternion rotatedQ = _left * q * Quaternion.Conjugate(_right);
        return new Vector4(rotatedQ.X, rotatedQ.Y, rotatedQ.Z, rotatedQ.W);
    }
}
