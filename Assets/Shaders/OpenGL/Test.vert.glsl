#version 460 core

// ARB_separate_shader_objects requires this to be declared
out gl_PerVertex {
	vec4 gl_Position;
	float gl_PointSize;
	float gl_ClipDistance[];
};

struct VertexData {
	float position[3];
	float color[3];
};

layout (binding = 0, std430) readonly buffer VertexBuffer {
	VertexData vertices[];
};

layout (binding = 1, std430) readonly buffer IndexBuffer {
	uint indices[];
};

uniform mat4 projection = mat4(1);
uniform mat4 view = mat4(1);
uniform mat4 model = mat4(1);

out vec4 fragColor;

vec3 getPosition() {
	VertexData vertex = vertices[indices[gl_VertexID]];
	return vec3(vertex.position[0], vertex.position[1], vertex.position[2]);
}

vec3 getColor() {
	VertexData vertex = vertices[indices[gl_VertexID]];
	return vec3(vertex.color[0], vertex.color[1], vertex.color[2]);
}

void main() {
	gl_Position = projection * view * model * vec4(getPosition(), 1);
	fragColor = vec4(getColor(), 1);
}