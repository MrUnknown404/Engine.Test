#version 460

layout (location = 0) in vec3 inPosition;

layout (location = 0) out uint outMaterialIndex;

layout (set = 0, binding = 0) uniform ProjectionModelBuffer {
	mat4 projection;
	mat4 model;
} projectionModelBuffer;

layout (push_constant, std430) uniform PushConsants {
	uint materialIndex;
} pushConsants;

void main() {
	gl_Position = projectionModelBuffer.projection * projectionModelBuffer.model * vec4(inPosition, 1);
	outMaterialIndex = pushConsants.materialIndex;
}