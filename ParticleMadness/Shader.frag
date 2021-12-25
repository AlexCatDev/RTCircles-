#version 300 es

precision mediump float;

uniform sampler2D u_Texture;

in vec2 v_TextureUV;
in vec2 v_Velocity;
in vec4 v_Color;

layout(location = 0) out vec4 FragColor;
layout(location = 1) out vec4 Velocity;

void main() {
	
	FragColor = texture(u_Texture, v_TextureUV) * v_Color;
	//Velocity = v_Velocity;
	Velocity = vec4(v_Velocity, 0.0, 1.0);
}

