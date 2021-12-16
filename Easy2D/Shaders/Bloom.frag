#version 300 es
//I really have no fucking clue what precision should be and how it matters so...
#if GL_ES
precision mediump float;
#endif

uniform float u_BloomThreshold;

uniform sampler2D u_Texture;
uniform sampler2D u_CombineTexture;
uniform bool u_Subtract;
uniform bool u_Combine;
uniform bool u_Blur;
uniform bool u_Final;

in vec2 v_TexCoordinate;
out vec4 FragColor;

vec4 upsample(sampler2D sampler2d){
	vec4 sampleScale = vec4(1.0);
	vec2 texelSize = 1.0 / vec2(textureSize(sampler2d, 0));
	vec4 d = texelSize.xyxy * vec4(1.0, 1.0, -1.0, 0.0) * sampleScale;
	vec2 uv = v_TexCoordinate;
    vec4 s;
    s =  texture(sampler2d, uv - d.xy);
    s += texture(sampler2d, uv - d.wy) * 2.0;
    s += texture(sampler2d, uv - d.zy);

    s += texture(sampler2d, uv + d.zw) * 2.0;
    s += texture(sampler2d, uv       ) * 4.0;
    s += texture(sampler2d, uv + d.xw) * 2.0;

    s += texture(sampler2d, uv + d.zy);
    s += texture(sampler2d, uv + d.wy) * 2.0;
    s += texture(sampler2d, uv + d.xy);

    return s * (1.0 / 16.0);
}

vec4 downsample(sampler2D sampler2d){
	vec2 texelSize = 1.0 / vec2(textureSize(sampler2d, 0));

	vec2 uv = v_TexCoordinate;

    vec4 A = texture(sampler2d, uv + texelSize * vec2(-1.0, -1.0));
    vec4 B = texture(sampler2d, uv + texelSize * vec2( 0.0, -1.0));
    vec4 C = texture(sampler2d, uv + texelSize * vec2( 1.0, -1.0));
    vec4 D = texture(sampler2d, uv + texelSize * vec2(-0.5, -0.5));
    vec4 E = texture(sampler2d, uv + texelSize * vec2( 0.5, -0.5));
    vec4 F = texture(sampler2d, uv + texelSize * vec2(-1.0,  0.0));
    vec4 G = texture(sampler2d, uv                               );
    vec4 H = texture(sampler2d, uv + texelSize * vec2( 1.0,  0.0));
    vec4 I = texture(sampler2d, uv + texelSize * vec2(-0.5,  0.5));
    vec4 J = texture(sampler2d, uv + texelSize * vec2( 0.5,  0.5));
    vec4 K = texture(sampler2d, uv + texelSize * vec2(-1.0,  1.0));
    vec4 L = texture(sampler2d, uv + texelSize * vec2( 0.0,  1.0));
    vec4 M = texture(sampler2d, uv + texelSize * vec2( 1.0,  1.0));

    vec2 div = (1.0 / 4.0) * vec2(0.5, 0.125);

    vec4 o = (D + E + I + J) * div.x;
    o += (A + B + G + F) * div.y;
    o += (B + C + H + G) * div.y;
    o += (F + G + L + K) * div.y;
    o += (G + H + M + L) * div.y;

    return o;
}

mat3x3 ACESInputMat =
mat3x3(
    0.59719, 0.35458, 0.04823,
    0.07600, 0.90834, 0.01566,
    0.02840, 0.13383, 0.83777
);

// ODT_SAT => XYZ => D60_2_D65 => sRGB
mat3x3 ACESOutputMat =
mat3x3(
     1.60475, -0.53108, -0.07367,
    -0.10208,  1.10813, -0.00605,
    -0.00327, -0.07276,  1.07602
);

vec3 RRTAndODTFit(vec3 v)
{
    vec3 a = v * (v + 0.0245786f) - 0.000090537f;
    vec3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
    return a / b;
}

vec3 ACESFitted(vec3 color)
{
    color = color * ACESInputMat;

    // Apply RRT and ODT
    color = RRTAndODTFit(color);

    color = color * ACESOutputMat;

    // Clamp to [0, 1]
    color = clamp(color, 0.0, 1.0);

    return color;
}

void main() {
	vec4 color;

	if(u_Blur) {
		color = downsample(u_Texture);
    } else if(u_Subtract) {
        color = texture(u_Texture, v_TexCoordinate);
        
		color.r = max(color.r - u_BloomThreshold, 0.0);
		color.g = max(color.g - u_BloomThreshold, 0.0);
		color.b = max(color.b - u_BloomThreshold, 0.0);
        

        //eew
        // check whether fragment output is higher than threshold, if so output as brightness color
        /*
        float brightness = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
        if(brightness > u_BloomThreshold)
            color = vec4(color.rgb, 1.0);
        else
            color = vec4(0.0, 0.0, 0.0, 1.0);
          */  
            
	} else if(u_Combine) {
		vec4 color2 = texture(u_CombineTexture, v_TexCoordinate);

		color = upsample(u_Texture);

		color = vec4(color.rgb + color2.rgb, 1.0);
	} else if(u_Final) {
        color = texture(u_Texture, v_TexCoordinate);

		vec4 inputBloom = texture(u_CombineTexture, v_TexCoordinate);

		vec3 tempColor = color.rgb + ACESFitted(inputBloom.rgb);
        //vec3 tempColor = ACESFitted(color.rgb + inputBloom.rgb);

		color = vec4(tempColor, 1.0);
        //Else we're writing to another buffer
	} else {
        color = texture(u_Texture, v_TexCoordinate);
    }

    FragColor = color;
}
