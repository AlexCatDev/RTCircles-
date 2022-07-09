#version 300 es
//I really have no fucking clue what precision should be and how it matters so...
precision mediump float;

uniform sampler2D u_SrcTexture;
uniform vec2 u_Direction;

in vec2 v_TexCoord;

out vec4 FragColor;

vec4 optimizedBlur5(sampler2D image, vec2 uv, vec2 resolution, vec2[5] blurCoordinates) {
    /*
        vec2 singleStepOffset = vec2(texelWidthOffset, texelHeightOffset);
	    blurCoordinates[0] = inputTextureCoordinate.xy;
	    blurCoordinates[1] = inputTextureCoordinate.xy + singleStepOffset * 1.407333;
	    blurCoordinates[2] = inputTextureCoordinate.xy - singleStepOffset * 1.407333;
	    blurCoordinates[3] = inputTextureCoordinate.xy + singleStepOffset * 3.294215;
	    blurCoordinates[4] = inputTextureCoordinate.xy - singleStepOffset * 3.294215
    */

    mediump vec4 sum = vec4(0.0);
	sum += texture(image, blurCoordinates[0]) * 0.204164;
	sum += texture(image, blurCoordinates[1]) * 0.304005;
	sum += texture(image, blurCoordinates[2]) * 0.304005;
	sum += texture(image, blurCoordinates[3]) * 0.093913;
	sum += texture(image, blurCoordinates[4]) * 0.093913;
	return sum;
}

vec4 blur13(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.411764705882353) * direction;
  vec2 off2 = vec2(3.2941176470588234) * direction;
  vec2 off3 = vec2(5.176470588235294) * direction;
  color += texture(image, uv) * 0.1964825501511404;
  color += texture(image, uv + (off1 / resolution)) * 0.2969069646728344;
  color += texture(image, uv - (off1 / resolution)) * 0.2969069646728344;
  color += texture(image, uv + (off2 / resolution)) * 0.09447039785044732;
  color += texture(image, uv - (off2 / resolution)) * 0.09447039785044732;
  color += texture(image, uv + (off3 / resolution)) * 0.010381362401148057;
  color += texture(image, uv - (off3 / resolution)) * 0.010381362401148057;
  return color;
}

vec4 blur9(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.3846153846) * direction;
  vec2 off2 = vec2(3.2307692308) * direction;
  color += texture(image, uv) * 0.2270270270;
  color += texture(image, uv + (off1 / resolution)) * 0.3162162162;
  color += texture(image, uv - (off1 / resolution)) * 0.3162162162;
  color += texture(image, uv + (off2 / resolution)) * 0.0702702703;
  color += texture(image, uv - (off2 / resolution)) * 0.0702702703;
  return color;
}

vec4 blur5(sampler2D image, vec2 uv, vec2 resolution, vec2 direction) {
  vec4 color = vec4(0.0);
  vec2 off1 = vec2(1.3333333333333333) * direction;
  color += texture(image, uv) * 0.29411764705882354;
  color += texture(image, uv + (off1 / resolution)) * 0.35294117647058826;
  color += texture(image, uv - (off1 / resolution)) * 0.35294117647058826;
  return color; 
}

vec4 DownsampleBox13Tap(sampler2D tex, vec2 uv, vec2 texelSize)
{
    vec4 A = texture(tex, uv + texelSize * vec2(-1.0, -1.0));
    vec4 B = texture(tex, uv + texelSize * vec2( 0.0, -1.0));
    vec4 C = texture(tex, uv + texelSize * vec2( 1.0, -1.0));
    vec4 D = texture(tex, uv + texelSize * vec2(-0.5, -0.5));
    vec4 E = texture(tex, uv + texelSize * vec2( 0.5, -0.5));
    vec4 F = texture(tex, uv + texelSize * vec2(-1.0,  0.0));
    vec4 G = texture(tex, uv                               );
    vec4 H = texture(tex, uv + texelSize * vec2( 1.0,  0.0));
    vec4 I = texture(tex, uv + texelSize * vec2(-0.5,  0.5));
    vec4 J = texture(tex, uv + texelSize * vec2( 0.5,  0.5));
    vec4 K = texture(tex, uv + texelSize * vec2(-1.0,  1.0));
    vec4 L = texture(tex, uv + texelSize * vec2( 0.0,  1.0));
    vec4 M = texture(tex, uv + texelSize * vec2( 1.0,  1.0));

    vec2 div = (1.0 / 4.0) * vec2(0.5, 0.125);

    vec4 o = (D + E + I + J) * div.x;
    o += (A + B + G + F) * div.y;
    o += (B + C + H + G) * div.y;
    o += (F + G + L + K) * div.y;
    o += (G + H + M + L) * div.y;

    return o;
}

// Standard box filtering
vec4 DownsampleBox4Tap(sampler2D tex, vec2 uv, vec2 texelSize)
{
    vec4 d = texelSize.xyxy * vec4(-1.0, -1.0, 1.0, 1.0);

    vec4 s;
    s =  texture(tex, uv + d.xy);
    s += texture(tex, uv + d.zy);
    s += texture(tex, uv + d.xw);
    s += texture(tex, uv + d.zw);

    return s * (1.0 / 4.0);
}

// 9-tap bilinear upsampler (tent filter)
vec4 UpsampleTent(sampler2D tex, vec2 uv, vec2 texelSize, vec4 sampleScale)
{
    vec4 d = texelSize.xyxy * vec4(1.0, 1.0, -1.0, 0.0) * sampleScale;

    vec4 s;
    s =  texture(tex, uv - d.xy);
    s += texture(tex, uv - d.wy) * 2.0;
    s += texture(tex, uv - d.zy);

    s += texture(tex, uv + d.zw) * 2.0;
    s += texture(tex, uv       ) * 4.0;
    s += texture(tex, uv + d.xw) * 2.0;
         
    s += texture(tex, uv + d.zy);
    s += texture(tex, uv + d.wy) * 2.0;
    s += texture(tex, uv + d.xy);

    return s * (1.0 / 16.0);
}

// Standard box filtering
vec4 UpsampleBox(sampler2D tex, vec2 uv, vec2 texelSize, vec4 sampleScale)
{
    vec4 d = texelSize.xyxy * vec4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

    vec4 s;
    s =  texture(tex, uv + d.xy);
    s += texture(tex, uv + d.zy);
    s += texture(tex, uv + d.xw);
    s += texture(tex, uv + d.zw);

    return s * (1.0 / 4.0);
}

void main() {
	//FragColor = blur13(texture(u_SrcTexture, v_TexCoord).xyz, 1.0);
	FragColor = blur9(u_SrcTexture, v_TexCoord, vec2(textureSize(u_SrcTexture, 0)), u_Direction);
	FragColor.a = 1.0;
}