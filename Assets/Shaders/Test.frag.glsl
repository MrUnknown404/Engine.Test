#version 460

#extension GL_EXT_descriptor_heap: require

layout (location = 0) in vec2 inUVs;
layout (location = 1) in vec3 inColor;

layout (location = 0) out vec4 outColor;

layout (descriptor_heap) uniform sampler samplers[];
layout (descriptor_heap) uniform texture2D textures[];

void main() {
	outColor = vec4(texture(sampler2D(textures[0], samplers[0]), inUVs).rgb * inColor, 1);
}