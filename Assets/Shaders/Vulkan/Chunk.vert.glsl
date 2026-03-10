#version 460
#extension GL_EXT_scalar_block_layout: enable

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inUVs;
layout (location = 2) in vec3 inNormal;

layout (location = 0) out vec2 outUVs;
layout (location = 1) out vec3 outNormal;

layout (binding = 0) uniform CameraBuffer {
	mat4 projection;
	mat4 view;
} cameraBuffer;

layout (binding = 1, scalar) readonly buffer PerChunkBuffer {
	ivec3 positions[];
} perChunkBuffer;

void main() {
	const int BlocksInChunk = 16;

	gl_Position = cameraBuffer.projection * cameraBuffer.view * vec4(inPosition + perChunkBuffer.positions[gl_DrawID] * BlocksInChunk, 1);
	outUVs = inUVs;
	outNormal = inNormal;
}