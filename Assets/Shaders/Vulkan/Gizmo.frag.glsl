#version 460

struct Material {
	vec4 baseColor;
} material;

layout (location = 0) flat in uint inMaterialIndex;

layout (location = 0) out vec4 outColor;

layout (set = 1, binding = 0, std140) readonly buffer MaterialBuffer {
	Material materials[];
} materialBuffer;

Material noMaterial() {
	return Material(vec4(1, 0, 1, 1));
}

void main() {
	Material m = inMaterialIndex == 0 ? noMaterial() : materialBuffer.materials[inMaterialIndex - 1];
	outColor = m.baseColor;
}