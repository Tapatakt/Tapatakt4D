#version 430 core

// Tapatakt4D - Fullscreen vertex shader

const vec2 positions[4] = vec2[](
    vec2(-1.0, -1.0),
    vec2( 1.0, -1.0),
    vec2(-1.0,  1.0),
    vec2( 1.0,  1.0)
);

const vec2 uvs[4] = vec2[](
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(0.0, 1.0),
    vec2(1.0, 1.0)
);

out vec2 fragCoord;

uniform ivec2 screenSize;

void main()
{
    gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
    
    // Pass actual pixel coordinates
    vec2 uv = uvs[gl_VertexID];
    fragCoord = uv * vec2(screenSize);
}
