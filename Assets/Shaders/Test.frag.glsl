#version 460

layout (location = 0) in vec2 inUVs;
layout (location = 1) in vec3 inColor;

layout (location = 0) out vec4 outColor;

layout (binding = 0) uniform sampler samplers[1];
layout (binding = 1) uniform texture2D textures[1];

void main() {
	outColor = vec4(texture(sampler2D(textures[0], samplers[0]), inUVs).rgb * inColor, 1);
}