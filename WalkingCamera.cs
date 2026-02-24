using OpenTK.Mathematics;
using System;

namespace Tapatakt4D;

/// <summary>
/// Walking camera constrained to a floor plane at Y=1.
/// Stores rotation in XZW subspace as a single 3D quaternion (treating W as Y).
/// Supports: XZ (yaw), XW (roll), ZW (pitch) rotations.
/// </summary>
public sealed class WalkingCamera : ICamera
{
    private Quaternion _xwzRotation = Quaternion.Identity; // 3D rotation in XZW space
    private float _pitch = 0.0f; // Additional YZ pitch (look up/down)

    /// <summary>
    /// Maximum pitch angle in radians. Default is 80 degrees.
    /// </summary>
    public float MaxPitch { get; }

    /// <summary>
    /// Camera position in 4D world space.
    /// </summary>
    public Vector4 Position { get; set; }

    /// <summary>
    /// Projection distance for perspective.
    /// </summary>
    public float ProjectionDistance { get; set; }

    /// <summary>
    /// Current XZW rotation quaternion.
    /// </summary>
    public Quaternion XzwRotation => _xwzRotation;

    /// <summary>
    /// Creates a new walking camera at the specified position.
    /// </summary>
    /// <param name="position">Camera position. Default is (0, 1, 8, 0).</param>
    /// <param name="projectionDistance">Projection distance for perspective. Default is 2.0.</param>
    /// <param name="maxPitchDegrees">Maximum pitch angle in degrees. Default is 80.</param>
    public WalkingCamera(Vector4? position = null, float projectionDistance = 2.0f, float maxPitchDegrees = 80.0f)
    {
        Position = position ?? new Vector4(0.0f, 1.0f, 8.0f, 0.0f);
        ProjectionDistance = projectionDistance;
        MaxPitch = maxPitchDegrees * MathF.PI / 180.0f;
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

    /// <summary>
    /// Gets the left quaternion for rotation (converted from 3D XZW rotation).
    /// </summary>
    public Quaternion LeftQuaternion => BuildDualQuaternions().left;

    /// <summary>
    /// Gets the right quaternion for rotation (converted from 3D XZW rotation).
    /// </summary>
    public Quaternion RightQuaternion => BuildDualQuaternions().right;

    /// <summary>
    /// Gets the inverse left quaternion for world-to-camera transform.
    /// </summary>
    public Quaternion LeftQuaternionInverse => Quaternion.Invert(LeftQuaternion);

    /// <summary>
    /// Gets the inverse right quaternion for world-to-camera transform.
    /// </summary>
    public Quaternion RightQuaternionInverse => Quaternion.Invert(RightQuaternion);

    /// <summary>
    /// Gets the forward direction projected to XZW plane and normalized.
    /// Used for movement - pitch doesn't affect XZW movement speed.
    /// </summary>
    public Vector4 ForwardXZW => NormalizeXZW(Forward);

    /// <summary>
    /// Gets the right direction projected to XZW plane and normalized.
    /// </summary>
    public Vector4 RightXZW => NormalizeXZW(Right);

    /// <summary>
    /// Gets the ana direction projected to XZW plane and normalized.
    /// </summary>
    public Vector4 AnaXZW => NormalizeXZW(Ana);

    /// <summary>
    /// Moves the camera like a first-person character:
    /// - XZW movement uses projected XZW directions (pitch doesn't affect speed)
    /// - Y movement uses world Y direction only
    /// </summary>
    public void Move(Vector4 delta)
    {
        Vector4 worldDelta = Vector4.Zero;

        // XZW movement: use projected, normalized directions
        if (delta.X != 0)
            worldDelta += RightXZW * delta.X;
        if (delta.Z != 0)
            worldDelta += ForwardXZW * delta.Z;
        if (delta.W != 0)
            worldDelta += AnaXZW * delta.W;

        // Y movement: use world Y only
        worldDelta.Y = delta.Y;

        Position += worldDelta;
    }

    /// <summary>
    /// Rotates the camera in XZW space.
    /// - angleYZ: yaw (XZ plane rotation)
    /// - angleXW: roll (XW plane rotation, W treated as Y)
    /// - angleZW: pitch (ZW plane rotation, W treated as Y)
    /// XY, XZ, YW rotations are ignored.
    /// </summary>
    public void Rotate(float angleXY = 0, float angleXZ = 0, float angleXW = 0,
                      float angleYZ = 0, float angleYW = 0, float angleZW = 0)
    {
        // Pitch: YZ rotation in 4D - is stored separately!
        if (angleYZ != 0)
        {
            _pitch += angleYZ;
            _pitch = Math.Clamp(_pitch, -MaxPitch, +MaxPitch);
        }
        // Yaw: XZ rotation in 4D, also treated as XZ yaw in 3D
        if (angleXZ != 0)
        {
            float half = angleXZ * 0.5f;
            Quaternion yaw3D = new(0, MathF.Sin(half), 0, MathF.Cos(half));
            _xwzRotation = yaw3D * _xwzRotation;
        }
        // XW rotation in 4D (treat W as Y in 3D, so it's XY roll in 3D)
        if (angleXW != 0)
        {
            float half = angleXW * 0.5f;
            Quaternion roll3D = new(0, 0, MathF.Sin(half), MathF.Cos(half));
            _xwzRotation = roll3D * _xwzRotation;
        }

        // ZW rotation in 4D (treat W as Y in 3D, so it's -YZ pitch in 3D)
        if (angleZW != 0)
        {
            float half = angleZW * 0.5f;
            Quaternion pitch3D = new(-MathF.Sin(half), 0, 0, MathF.Cos(half));
            _xwzRotation = pitch3D * _xwzRotation;
        }

        // Normalize the rotation quaternion
        _xwzRotation = Quaternion.Normalize(_xwzRotation);
    }

    /// <summary>
    /// Converts the 3D XZW rotation quaternion to 4D dual quaternions.
    /// </summary>
    private (Quaternion left, Quaternion right) BuildDualQuaternions()
    {
        float yz3d = _xwzRotation.X;
        float xz3d = _xwzRotation.Y;
        float xy3d = _xwzRotation.Z;
        float w3d = _xwzRotation.W;
        Quaternion left = new(-xy3d, xz3d, -yz3d, w3d);
        Quaternion right = new(xy3d, xz3d, yz3d, w3d);
        /*
        // The 3D quaternion _xwzRotation represents rotation in virtual (X, W, Z) space
        // where we treat it like (X, Y, Z) with W playing the role of Y
        Quaternion left = new(_xwzRotation.X, _xwzRotation.Z, _xwzRotation.Y, _xwzRotation.W);
        Quaternion right = new(_xwzRotation.X, -_xwzRotation.Z, -_xwzRotation.Y, _xwzRotation.W);
        */
        float half = _pitch * 0.5f;
        Quaternion pitch = new(MathF.Sin(half), 0, 0, MathF.Cos(half));
        left = (pitch * left).Normalized();
        right = (right * pitch).Normalized();
        return (left, right);
    }

    /// <summary>
    /// Rotates a vector using the current dual quaternion representation.
    /// </summary>
    private Vector4 RotateVector(Vector4 v)
    {
        (Quaternion left, Quaternion right) = BuildDualQuaternions();
        Quaternion q = new Quaternion(v.X, v.Y, v.Z, v.W);
        Quaternion rotatedQ = left * q * Quaternion.Conjugate(right);
        return new Vector4(rotatedQ.X, rotatedQ.Y, rotatedQ.Z, rotatedQ.W);
    }

    /// <summary>
    /// Projects a vector to XZW plane (zeros Y) and normalizes.
    /// </summary>
    private static Vector4 NormalizeXZW(Vector4 v)
    {
        Vector4 projected = new(v.X, 0.0f, v.Z, v.W);
        return projected.Normalized();
    }
}
