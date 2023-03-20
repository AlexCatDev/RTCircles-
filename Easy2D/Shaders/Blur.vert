#version 300 es

uniform mat4 u_Projection;
uniform vec2 u_QuadSize;

in vec2 a_Position;
in vec2 a_TexCoord;

out vec2 v_TexCoord;

void main() {
	gl_Position = vec4(a_Position * u_QuadSize, 0.0, 1.0) * u_Projection;

	v_TexCoord = a_TexCoord;
}