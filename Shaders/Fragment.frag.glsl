#version 430 core

// Tapatakt4D - Fragment shader for edge rendering with alternation

in vec2 fragCoord;  // Pixel coordinates

out vec4 outColor;

struct EdgeProjection
{
    vec4 start;  // xyzw = screen xy, distance from camera, original W
    vec4 end;
    float thickness;
};

layout(std430, binding = 0) readonly buffer ProjectionBuffer
{
    EdgeProjection projections[];
};

layout(std430, binding = 1) readonly buffer TileCountBuffer
{
    uint tileCounts[];
};

layout(std430, binding = 2) readonly buffer TileDataBuffer
{
    uint tileEdgeIds[];
};

uniform ivec2 screenSize;
uniform ivec2 gridSize;
uniform int tileSize;
uniform int maxEdgesPerTile;
uniform int frameNumber;

// Distance from point p to line segment ab
float PointLineDistance(vec2 p, vec2 a, vec2 b)
{
    vec2 pa = p - a;
    vec2 ba = b - a;
    
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

// Maximum W ratio - at distance d, visible W range is [-d*MAX_W_RATIO, d*MAX_W_RATIO]
// This makes W clipping perspective-correct like X/Y
const float MAX_W_RATIO = 0.5;

// Map w-coordinate to color: red(ana) -> green(center) -> blue(kata)
vec3 WToColor(float w, float dist)
{
    // Perspective-correct visible W range
    // At distance d from camera, max visible |w| is d * MAX_W_RATIO
    float maxWAtDist = dist * MAX_W_RATIO;
    
    // Clamp w to visible range for color mapping
    // w = -maxWAtDist -> blue (kata)
    // w = 0 -> green (center)
    // w = +maxWAtDist -> red (ana)
    float normalized = clamp(w / maxWAtDist, -1.0, 1.0);
    
    if (normalized > 0.0)
    {
        // +w: red to green
        float t = 1.0 - normalized;
        return vec3(1.0 - t, t, 0.0);
    }
    else
    {
        // -w: green to blue
        float t = 1.0 + normalized;
        return vec3(0.0, t, 1.0 - t);
    }
}

void main()
{
    vec2 pixel = fragCoord;
    
    // Find which tile this pixel belongs to
    ivec2 tile = ivec2(pixel) / tileSize;
    
    // Bounds check
    if (tile.x < 0 || tile.y < 0 || tile.x >= gridSize.x || tile.y >= gridSize.y)
    {
        outColor = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }
    
    int tileIdx = tile.y * gridSize.x + tile.x;
    uint count = tileCounts[tileIdx];
    
    if (count == 0u)
    {
        outColor = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }
    
    // Clamp to max edges per tile
    count = min(count, uint(maxEdgesPerTile));
    
    // Collect actual hits at this pixel
    // Use a small fixed-size array (GPU stack)
    // Store w and distance for perspective-correct clipping
    float hitW[32];
    float hitDist[32];
    uint hitCount = 0u;
    
    uint baseIdx = uint(tileIdx) * uint(maxEdgesPerTile);
    
    for (uint i = 0u; i < count && hitCount < 32u; i++)
    {
        uint edgeId = tileEdgeIds[baseIdx + i];
        EdgeProjection proj = projections[edgeId];
        
        float distPixelToProjection = PointLineDistance(pixel, proj.start.xy, proj.end.xy);
        
        // Calculate interpolated distance at closest point first (for perspective thickness)
        vec2 pa = pixel - proj.start.xy;
        vec2 ba = proj.end.xy - proj.start.xy;
        
        float baLenSq = dot(ba, ba);
        float t = 0.0;
        
        if (baLenSq > 0.0001)
        {
            t = clamp(dot(pa, ba) / baLenSq, 0.0, 1.0);
        }
        
        float distCameraToEdgePoint = mix(proj.start.z, proj.end.z, t);
        
        // Make far edges thinner - thickness is divided by distance
        // Add a small value to avoid division by zero
        if (distPixelToProjection <= proj.thickness) // / (0.1 + distAtPixel * 0.1))
        {
            float w = mix(proj.start.w, proj.end.w, t);
            
            // Perspective-correct W clipping
            // At distance d from camera, max visible |w| is d * MAX_W_RATIO
            float maxWAtDist = distCameraToEdgePoint * MAX_W_RATIO;
            if (abs(w) > maxWAtDist)
                continue;
            
            hitW[hitCount] = w;
            hitDist[hitCount] = distCameraToEdgePoint;
            hitCount++;
        }
    }
    
    if (hitCount == 0u)
    {
        outColor = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }
    
    // Select edge based on frame number
    uint selected = uint(frameNumber) % hitCount;
    float w = hitW[selected];
    float dist = hitDist[selected];
    
    outColor = vec4(WToColor(w, dist), 1.0);
}
