#version 300 es

uniform mat4 u_Projection;
uniform float u_OsuRadius;

in vec3 a_InstanceCircleVertex;
in vec2 a_Offset;

void main() {	
	gl_Position = vec4((a_InstanceCircleVertex.xy * u_OsuRadius) + a_Offset, a_InstanceCircleVertex.z, 1) * u_Projection;
}