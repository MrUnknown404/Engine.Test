#version 460

layout (location = 0) in vec2 inUVs;
layout (location = 1) in vec3 inNormal;

layout (location = 0) out vec4 outColor;

layout (binding = 2) uniform sampler2D texSampler;

layout (push_constant, std430) uniform PushConsants {
	uint lightColor;
	float lightDirection[3];
} pushConsants;

vec3 unpackLightColor() {
	uint r = (pushConsants.lightColor >> 24u) & 0xFFu;
	uint g = (pushConsants.lightColor >> 16u) & 0xFFu;
	uint b = (pushConsants.lightColor >> 8u) & 0xFFu;
	// uint a = (pushConsants.lightColor >> 0) & 0xFFu;

	return vec3(r / 255.0, g / 255.0, b / 255.0);
}

vec3 getLightDirection() {
	float lightDirection[] = pushConsants.lightDirection;
	return vec3(lightDirection[0], lightDirection[1], lightDirection[2]);
}

void main() {
	const float ambientStrength = 0.1;

	vec3 lightColor = unpackLightColor();

	vec3 ambient = ambientStrength * lightColor;
	vec3 diffuse = max(dot(inNormal, getLightDirection()), 0) * lightColor;

	vec3 result = (ambient + diffuse) * texture(texSampler, inUVs).rgb;

	outColor = vec4(result, 1);
}