//pik
#version 300 es

in vec2 a_Position;
in vec2 a_TexCoordinate;

uniform mat4 u_Projection;

out vec2 v_TexCoordinate;

void main() {
	gl_Position = vec4(a_Position, 0.0, 1.0) * u_Projection;

	v_TexCoordinate = a_TexCoordinate;
}