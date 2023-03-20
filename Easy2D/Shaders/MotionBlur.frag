#version 300 es
precision mediump float;

uniform sampler2D u_Texture;
uniform sampler2D u_CombineTexture;

uniform float u_NewPercentage;

in vec2 v_TexCoordinate;
out vec4 FragColor;

//more like ghosting effect
void main() {
	//Color is what we're actually writing, to, and is also the previous frame, and the moving average
	vec4 color = texture(u_Texture, v_TexCoordinate);
	//Color2 is our new frame texture
	vec4 color2 = texture(u_CombineTexture, v_TexCoordinate);
	float orignal = 1.0 - u_NewPercentage;

	FragColor.r = color.r * orignal + color2.r * u_NewPercentage;
	FragColor.g = color.g * orignal + color2.g * u_NewPercentage;
	FragColor.b = color.b * orignal + color2.b * u_NewPercentage;
	FragColor.a = 1.0;
}
