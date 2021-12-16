#version 300 es

uniform mat4 u_Projection;

in vec2 a_Position;
in vec2 a_TexCoord;

out vec2 v_TexCoord;

void main() {
	gl_Position = vec4(a_Position, 0.0, 1.0) * u_Projection;

	v_TexCoord = a_TexCoord;
}