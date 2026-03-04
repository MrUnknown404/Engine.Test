#version 460

struct PerChunkData {
	int position[3];
};

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inUVs;

layout (location = 0) out vec2 fragUVs;

layout (binding = 0) uniform CameraBuffer {
	mat4 projection;
	mat4 view;
} cameraBuffer;

layout (binding = 1, std430) readonly buffer PerChunkBuffer {
	PerChunkData data[];
} perChunkBuffer;

ivec3 getChunkPosition() {
	PerChunkData chunkData = perChunkBuffer.data[gl_DrawID];
	return ivec3(chunkData.position[0], chunkData.position[1], chunkData.position[2]);
}

void main() {
	const int BlocksInChunk = 16;

	gl_Position = cameraBuffer.projection * cameraBuffer.view * vec4(inPosition + getChunkPosition() * BlocksInChunk, 1);

	fragUVs = inUVs;
}