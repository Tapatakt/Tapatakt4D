#version 430 core

// Tapatakt4D - Compute shader for edge projection and tile rasterization

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
// z = camera-space Z (for perspective-correct W clipping)
// w = original W (for color)
struct EdgeProjection
{
    vec4 start;  // xyzw = screen xy, camera Z, original W
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

// Camera data
uniform mat4 cameraRotationInv;  // Inverse camera rotation (world to camera)
uniform vec4 cameraPosition;     // Camera position (world space)
uniform float projectionDist;    // Distance for perspective projection
uniform ivec2 screenSize;
uniform ivec2 gridSize;          // Tile grid dimensions
uniform int tileSize;            // Pixels per tile
uniform int maxEdgesPerTile;
uniform int edgeCount;

// Transform world space to camera space
vec4 WorldToCamera(vec4 worldPos)
{
    // Apply inverse rotation, then subtract camera position
    return cameraRotationInv * (worldPos - cameraPosition);
}

// Project 4D point to screen
// Returns: vec4(screen_x, screen_y, depth, original_w)
vec4 Project4D(vec4 p)
{
    // Use actual Euclidean distance from camera for projection
    // This works regardless of which direction the camera looks
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
    // Note: p.z is still passed for W clipping consistency
    return vec4(x, y, dist, p.w);
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
    
    // Project to screen space
    vec4 start = Project4D(startCamera);
    vec4 end = Project4D(endCamera);
    
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
