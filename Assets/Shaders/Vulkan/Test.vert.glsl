#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inUVs;
layout (location = 2) in vec3 inColor;

layout (location = 0) out vec2 fragUVs;
layout (location = 1) out vec3 fragColor;

layout (binding = 0) uniform CameraBuffer {
	mat4 projection;
	mat4 view;
} cameraBuffer;

layout (binding = 1, std140) readonly buffer InstanceBuffers {
	mat4 models[];
} instanceBuffers;

void main() {
	gl_Position = cameraBuffer.projection * cameraBuffer.view * instanceBuffers.models[gl_InstanceIndex] * vec4(inPosition, 1);
	fragUVs = inUVs;
	fragColor = inColor;
}