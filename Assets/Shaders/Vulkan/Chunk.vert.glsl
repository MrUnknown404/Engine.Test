#version 460

layout (location = 0) in vec3 inPosition;

layout (binding = 0) uniform CameraBuffer {
	mat4 projection;
	mat4 view;
} cameraBuffer;

void main() {
	gl_Position = cameraBuffer.projection * cameraBuffer.view * vec4(inPosition, 1);
}