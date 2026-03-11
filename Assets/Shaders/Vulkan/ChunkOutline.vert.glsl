#version 460
#extension GL_EXT_scalar_block_layout: enable

layout (location = 0) in vec3 inPosition;
layout (location = 1) in uint inColor;

layout (location = 0) out uint outColor;

layout (binding = 0) uniform CameraBuffer {
	mat4 projection;
	mat4 view;
} cameraBuffer;

layout (push_constant, scalar) uniform PushConsants {
	ivec3 position;
} pushConsants;

void main() {
	const int BlocksInChunk = 16;

	gl_Position = cameraBuffer.projection * cameraBuffer.view * vec4(inPosition + pushConsants.position * BlocksInChunk, 1);
	outColor = inColor;
}