#version 430 core

// Tapatakt4D - Compute shader for edge projection and tile rasterization
// Uses dual quaternion representation for camera rotation

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

// Input: 4D edges in world space
struct Edge4D
{
    vec4 start;
    vec4 end;
    float thickness;
    float _padding[3];
};

// Output: Projected edges
// x,y = screen position
// z = distance from camera (for perspective-correct W clipping)
// w = original W (for color)
struct EdgeProjection
{
    vec4 start;  // xyzw = screen xy, distance, original W
    vec4 end;
    float thickness;
};

layout(std430, binding = 0) readonly buffer Edge4DBuffer
{
    Edge4D edges4d[];
};

layout(std430, binding = 1) writeonly buffer EdgeProjectionBuffer
{
    EdgeProjection projections[];
};

// Per-tile edge lists
layout(std430, binding = 2) buffer TileCountBuffer
{
    uint tileCounts[];
};

layout(std430, binding = 3) buffer TileDataBuffer
{
    uint tileEdgeIds[];  // Flattened: tileIndex * maxEdgesPerTile + slot
};

// Camera data - dual quaternion representation
// Rotation of vector v: v' = left * v * right
uniform vec4 cameraLeftQuat;       // Left inverted camera rotation quaternion (xyzw)
uniform vec4 cameraRightQuat;      // Right inverted camera rotation quaternion (xyzw)
uniform vec4 cameraPosition;       // Camera position (world space)
uniform float projectionDist;      // Distance for perspective projection
uniform ivec2 screenSize;
uniform ivec2 gridSize;            // Tile grid dimensions
uniform int tileSize;              // Pixels per tile
uniform int maxEdgesPerTile;
uniform int edgeCount;

// Quaternion multiplication: a * b
vec4 QuatMul(vec4 a, vec4 b)
{
    return vec4(
        a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
        a.w * b.y - a.x * b.z + a.y * b.w + a.z * b.x,
        a.w * b.z + a.x * b.y - a.y * b.x + a.z * b.w,
        a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z
    );
}

// Quaternion conjugation
vec4 Conjugation(vec4 q)
{
    return vec4(-q.xyz, q.w);
}

// Rotate vector v by quaternion pair (left, right): v' = left * v * conjugation(right)
vec4 RotateByQuatPair(vec4 v, vec4 left, vec4 right)
{
    return QuatMul(QuatMul(left, v), Conjugation(right));
}

// Transform world space to camera space using dual quaternion
vec4 WorldToCamera(vec4 worldPos)
{
    vec4 delta = worldPos - cameraPosition;
    return RotateByQuatPair(delta, cameraLeftQuat, cameraRightQuat);
}

// Project 4D point to screen
// Returns: vec4(screen_x, screen_y, distance, original_w)
vec4 Project4D(vec4 p)
{
    // Use actual Euclidean distance from camera for projection
    float dist = length(p);
    
    // Clamp to avoid division by zero
    if (dist < 0.001)
    {
        dist = 0.001;
    }
    
    // projectionDist acts as focal length (controls FOV)
    float scale = projectionDist / dist;
    float x = p.x * scale;
    float y = p.y * scale;
    
    // Map from normalized device coords to screen coords
    x = (x + 1.0) * 0.5 * float(screenSize.x);
    y = (y + 1.0) * 0.5 * float(screenSize.y);
    
    // Return screen xy + distance + original W (for color)
    return vec4(x, y, dist, p.w);
}

// Clip edge to near plane (z = 0)
// Returns true if edge is visible after clipping
bool ClipEdge(vec4 start, vec4 end, out vec4 clippedStart, out vec4 clippedEnd)
{
    float startZ = start.z;
    float endZ = end.z;
    
    // Both behind camera - cull
    if (startZ > 0.0 && endZ > 0.0)
        return false;
    
    // Both in front - keep as-is
    if (startZ <= 0.0 && endZ <= 0.0)
    {
        clippedStart = start;
        clippedEnd = end;
        return true;
    }
    
    // One in front, one behind - clip to z = 0
    float t = startZ / (startZ - endZ);  // Interpolation factor
    vec4 clippedPoint = mix(start, end, t);
    clippedPoint.z = 0.0;  // Exactly at near plane
    
    if (startZ <= 0.0)
    {
        // Start in front, end behind
        clippedStart = start;
        clippedEnd = clippedPoint;
    }
    else
    {
        // Start behind, end in front
        clippedStart = clippedPoint;
        clippedEnd = end;
    }
    return true;
}

void main()
{
    uint edgeId = gl_GlobalInvocationID.x;
    if (int(edgeId) >= edgeCount)
    {
        return;
    }
    
    Edge4D e = edges4d[edgeId];
    
    // Transform from world space to camera space
    vec4 startCamera = WorldToCamera(e.start);
    vec4 endCamera = WorldToCamera(e.end);
    
    // Clip to near plane
    vec4 clippedStart, clippedEnd;
    if (!ClipEdge(startCamera, endCamera, clippedStart, clippedEnd))
    {
        // Both points behind camera - mark as invalid
        projections[edgeId].start = vec4(-1.0, -1.0, -1.0, -1.0);
        projections[edgeId].end = vec4(-1.0, -1.0, -1.0, -1.0);
        projections[edgeId].thickness = 0.0;
        return;
    }
    
    // Project to screen space
    vec4 start = Project4D(clippedStart);
    vec4 end = Project4D(clippedEnd);
    
    // Store projection for fragment shader
    projections[edgeId].start = start;
    projections[edgeId].end = end;
    projections[edgeId].thickness = e.thickness;
    
    // Find bounding box in screen space (expand by thickness)
    vec2 minPos = min(start.xy, end.xy) - vec2(e.thickness);
    vec2 maxPos = max(start.xy, end.xy) + vec2(e.thickness);
    
    // Use floor division to handle negative coordinates correctly
    ivec2 tileStart = ivec2(floor(minPos / float(tileSize)));
    ivec2 tileEnd = ivec2(floor(maxPos / float(tileSize))) + 1;
    
    // Clamp to grid bounds
    tileStart = max(tileStart, ivec2(0));
    tileEnd = min(tileEnd, gridSize);
    
    // Add edge to all overlapping tiles
    for (int ty = tileStart.y; ty < tileEnd.y; ty++)
    {
        for (int tx = tileStart.x; tx < tileEnd.x; tx++)
        {
            int tileIdx = ty * gridSize.x + tx;
            uint slot = atomicAdd(tileCounts[tileIdx], 1u);
            
            if (int(slot) < maxEdgesPerTile)
            {
                uint dataIdx = uint(tileIdx) * uint(maxEdgesPerTile) + slot;
                tileEdgeIds[dataIdx] = edgeId;
            }
        }
    }
}
