#version 460

layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inUVs;
layout (location = 2) in vec3 inColor;

layout (location = 0) out vec2 fragUVs;
layout (location = 1) out vec3 fragColor;

layout (binding = 0) uniform UniformBufferObject {
	mat4 projection;
	mat4 view;
	mat4 model;
} ubo;

void main() {
	gl_Position = ubo.projection * ubo.view * ubo.model * vec4(inPosition, 1);
	fragUVs = inUVs;
	fragColor = inColor;
}