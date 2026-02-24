using OpenTK.Mathematics;

namespace Tapatakt4D;

/// <summary>
/// Interface for 4D camera implementations.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Camera position in 4D world space.
    /// </summary>
    Vector4 Position { get; set; }

    /// <summary>
    /// Projection distance for perspective.
    /// </summary>
    float ProjectionDistance { get; set; }

    /// <summary>
    /// Gets the forward direction vector (-Z in camera space).
    /// </summary>
    Vector4 Forward { get; }

    /// <summary>
    /// Gets the right direction vector (+X in camera space).
    /// </summary>
    Vector4 Right { get; }

    /// <summary>
    /// Gets the up direction vector (+Y in camera space).
    /// </summary>
    Vector4 Up { get; }

    /// <summary>
    /// Gets the ana direction vector (+W in camera space).
    /// </summary>
    Vector4 Ana { get; }

    /// <summary>
    /// Gets the left quaternion for rotation.
    /// </summary>
    Quaternion LeftQuaternion { get; }

    /// <summary>
    /// Gets the right quaternion for rotation.
    /// </summary>
    Quaternion RightQuaternion { get; }

    /// <summary>
    /// Gets the inverse left quaternion for world-to-camera transform.
    /// </summary>
    Quaternion LeftQuaternionInverse { get; }

    /// <summary>
    /// Gets the inverse right quaternion for world-to-camera transform.
    /// </summary>
    Quaternion RightQuaternionInverse { get; }

    /// <summary>
    /// Moves the camera by the specified delta in camera space.
    /// </summary>
    void Move(Vector4 delta);

    /// <summary>
    /// Rotates the camera by the specified angles in the given planes.
    /// </summary>
    void Rotate(float angleXY = 0, float angleXZ = 0, float angleXW = 0,
                float angleYZ = 0, float angleYW = 0, float angleZW = 0);
}
