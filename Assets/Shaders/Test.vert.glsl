#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inUVs;
layout (location = 2) in vec3 inColor;

layout (location = 0) out vec2 outUVs;
layout (location = 1) out vec3 outColor;

void main() {
	gl_Position = vec4(inPosition, 1);
	outUVs = inUVs;
	outColor = inColor;
}