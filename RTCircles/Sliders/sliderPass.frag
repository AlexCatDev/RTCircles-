#version 300 es
//I really have no fucking clue what precision should be and how it matters so...
#if GL_ES
precision mediump float;
#endif

//MIT License
//Code stolen from Github.com/Wieku/danser-go thanks for this smexy slider shader owo <3
//Changed some variable names and made it opengl es friendly

#define borderStart 0.06640625 // 34/512
#define baseBorderWidth 0.126953125 // 65/512
#define blend 0.01

#define maxBorderWidth 1.0 - borderStart

#define slope (maxBorderWidth - baseBorderWidth) / 9.0

uniform vec4 u_BorderColorOuter;
uniform vec4 u_BorderColorInner;

uniform vec4 u_TrackColorOuter;
uniform vec4 u_TrackColorInner;

uniform vec4 u_ShadowColor;

uniform sampler2D u_Texture;

uniform sampler2D u_SliderGradient;

uniform float u_Scale;

uniform float u_BorderWidth;

in vec2 v_TexCoord;

out vec4 FragColor;

float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
{
   return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
}

void useSliderGradient()
{
    float r = texture(u_Texture, v_TexCoord).r;
    //Map the depth value from 0.5 -> 1.0 to 1.0 -> 0.0, and use it as the x coordinate of the gradient texture
    //float x = Map(r, 1.0, 0.5, 0.0, 1.0);

    //Map the depth value from 0.5 -> 1.0 to 1.0 -> 0.0, and use it as the x coordinate of the gradient texture
    float x = 1.0 - (r - 0.5) * 2.0;

    vec2 texCoord = vec2(x, 0.0);
    FragColor = texture(u_SliderGradient, texCoord); 
}

void main()
{
    float distance = texture(u_Texture, v_TexCoord).r * 2.0 - 1.0;

    if (distance >= u_Scale) {
        discard;
    }

    float distance_inv = 1.0 - distance / u_Scale;

    //what does this do?
    //gl_FragDepth = 1.0 - distance_inv * u_BorderColorOuter.a;

    vec4 borderColorOuter = u_BorderColorOuter;
    vec4 borderColorInner = u_BorderColorInner;
    //Original was vec4 outerShadow = vec4(vec3(0.0), 0.5 * distance_inv / borderStart * borderColorInner.a);
    vec4 outerShadow = vec4(vec3(u_ShadowColor.r, u_ShadowColor.g, u_ShadowColor.b), u_ShadowColor.a * distance_inv / borderStart * borderColorInner.a);

    vec4 bodyColorOuter = u_TrackColorOuter;
    vec4 bodyColorInner = u_TrackColorInner;

    float borderWidthScaled = u_BorderWidth < 1.0 ? u_BorderWidth * baseBorderWidth : (u_BorderWidth - 1.0) * slope + baseBorderWidth;
    float borderMid = borderStart + borderWidthScaled / 2.0;
    float borderEnd = borderStart + borderWidthScaled;

    vec4 borderColorMix = mix(borderColorOuter, borderColorInner, smoothstep(borderMid - borderWidthScaled/4.0, borderMid + borderWidthScaled/4.0, distance_inv));
    vec4 bodyColorMix = mix(bodyColorOuter, bodyColorInner, (distance_inv - borderEnd) / (1.0 - borderEnd));

    if (u_BorderWidth < 0.01) {
        borderColorMix = outerShadow;
    }

    if (u_BorderWidth > 9.99) {
        bodyColorMix = borderColorMix;
    }

    if (distance_inv <= borderStart - blend) {
        FragColor = outerShadow;
    }

    if (distance_inv > borderStart-blend && distance_inv < borderStart+blend) {
        FragColor = mix(outerShadow, borderColorMix, (distance_inv - (borderStart - blend)) / (2.0 * blend));
    }

    if (distance_inv > borderStart+blend && distance_inv <= borderEnd-blend) {
        FragColor = borderColorMix;
    }

    if (distance_inv > borderEnd-blend && distance_inv < borderEnd+blend) {
        FragColor = mix(borderColorMix, bodyColorMix, (distance_inv - (borderEnd - blend)) / (2.0 * blend));
    }

    if (distance_inv > borderEnd + blend) {
        FragColor = bodyColorMix;
    }
}
