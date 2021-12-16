#version 300 es

precision mediump float;

uniform mat4 u_Projection;

layout(location = 0) in vec2 a_Position;
layout(location = 1) in vec4 a_Color;

//out vec2 v_TexCoordinate;
out vec4 v_Color;
//flat out int v_TextureSlot;

vec2 rotate_around(vec2 point, vec2 origin, float angle) {
	vec2 p1 = point - origin;
	float coss = cos(angle);
	float sinn = sin(angle);

	vec2 p2 = vec2(coss * p1.x - sinn * p1.y, sinn * p1.x + coss * p1.y);
	return p2 + origin;
}

//jeg ved ikk overhovedet ikk hvad jeg laver matematik????????????????????????????????

void main() {
	//gl_Position = vec4(rotate_around(a_Position, a_Origin, a_Rotation), 0.0, 1.0) * u_Projection;
	gl_Position = vec4(a_Position, 0.0, 1.0) * u_Projection;

	//v_TexCoordinate = a_TexCoordinate;
	v_Color = a_Color;
	//v_TextureSlot = a_TextureSlot;
}