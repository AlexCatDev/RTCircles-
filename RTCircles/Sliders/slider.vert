﻿#version 300 es

uniform mat4 u_Projection;

in vec3 a_Position;

void main() {
	gl_Position = u_Projection * vec4(a_Position.x, a_Position.y, a_Position.z, 1);
}