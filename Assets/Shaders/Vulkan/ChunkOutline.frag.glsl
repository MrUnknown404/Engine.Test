#version 460

layout (location = 0) flat in uint inColor;

layout (location = 0) out vec4 outColor;

void main() {
	uint r = (inColor >> 24) & 0xFF;
	uint g = (inColor >> 16) & 0xFF;
	uint b = (inColor >> 8) & 0xFF;
	// uint a = (inColor >> 0) & 0xFF;

	outColor = vec4(r / 255.0, g / 255.0, b / 255.0, 1);
}