#version 300 es

precision mediump float;

uniform sampler2D u_VelocityTexture;
uniform sampler2D u_FrameTexture;

uniform vec2 u_Direction;

in vec2 v_TextureUV;
in vec2 v_Velocity;
in vec4 v_Color;

out vec4 FragColor;

vec4 blur13(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.411764705882353) * direction;
  vec2 off2 = vec2(3.2941176470588234) * direction;
  vec2 off3 = vec2(5.176470588235294) * direction;
  color += texture2D(image, uv) * 0.1964825501511404;
  color += texture2D(image, uv + (off1 / resolution)) * 0.2969069646728344;
  color += texture2D(image, uv - (off1 / resolution)) * 0.2969069646728344;
  color += texture2D(image, uv + (off2 / resolution)) * 0.09447039785044732;
  color += texture2D(image, uv - (off2 / resolution)) * 0.09447039785044732;
  color += texture2D(image, uv + (off3 / resolution)) * 0.010381362401148057;
  color += texture2D(image, uv - (off3 / resolution)) * 0.010381362401148057;
  return color;
}

void main() {
	vec2 size = vec2(textureSize(u_FrameTexture, 0));
	FragColor = blur13(u_FrameTexture, v_TextureUV, size, u_Direction * texture2D(u_VelocityTexture, v_TextureUV).rg);
	//FragColor = texture(u_Texture, v_TextureUV) * v_Color;
}