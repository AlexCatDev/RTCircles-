#version 300 es

#if GL_ES
precision mediump float;
#endif

uniform vec4 u_Color;

out vec4 FragColor;

void main() {
	FragColor = u_Color;
}