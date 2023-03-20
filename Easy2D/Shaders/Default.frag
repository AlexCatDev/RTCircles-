#version 300 es
precision mediump float;

//Programmatically insert the correct amount of textures supported on this system
//Gets replaced when this file is loaded
//#uniform sampler2D u_Textures[];

uniform vec4 u_FinalColorMult;
uniform vec3 u_FinalColorAdd;

in vec2 v_TexCoordinate;
in vec4 v_Color;
flat in int v_TextureSlot;

out vec4 FragColor;

float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
{
	return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
}
/*
float roundedRectSDF(vec2 centerPosition, vec2 size, float radius)
{
    return length(max(abs(centerPosition) - (size / 2.0) + radius, 0.0)) - radius;
}
*/
float roundedBoxSDF(vec2 CenterPosition, vec2 Size, float Radius)
{
    return length(max(abs(CenterPosition) - Size + Radius, 0.0)) - Radius;
}

vec3 light(vec2 lightPos, float lightRadius, vec2 normal, vec3 color)
{
    vec2 pixelPos = gl_FragCoord.xy;

    vec2 toLight = (lightPos - pixelPos);

    vec3 lightcolor = color;

    // This computes how much is the pixel lit based on where it faces
    float brightness = clamp(dot(normalize(toLight), normal), 0.0, 1.0);

    // If it faces towards the light it is lit fully, if it is perpendicular
    // to the direction towards the light then it is not lit at all.

    // This reduces the brightness based on the distance form the light and the light's radius
    brightness *= clamp(1.0 - (length(toLight) / lightRadius), 0.0, 1.0);
    // The final color of the pixel.
    return lightcolor * brightness;
}

uniform vec3 u_BorderColorOuter;
uniform vec3 u_BorderColorInner;

uniform vec3 u_TrackColorOuter;
uniform vec3 u_TrackColorInner;

uniform vec4 u_ShadowColor;

uniform float u_BorderWidth;

vec4 slider(vec4 sliderTexture) {

    #define borderStart 0.06640625 // 34/512
    #define baseBorderWidth 0.126953125 // 65/512
    #define blend 0.01

    #define maxBorderWidth 1.0 - borderStart

    #define slope (maxBorderWidth - baseBorderWidth) / 9.0

    float distance = sliderTexture.r * 2.0 - 1.0;

    float distance_inv = 1.0 - distance;

    vec4 borderColorOuter = vec4(u_BorderColorOuter.rgb, v_Color.a);
    vec4 borderColorInner = vec4(u_BorderColorInner.rgb, v_Color.a);

    //Original was vec4 outerShadow = vec4(vec3(0.0), 0.5 * distance_inv / borderStart * borderColorInner.a);
    vec4 outerShadow = vec4(u_ShadowColor.rgb, u_ShadowColor.a * distance_inv / borderStart * borderColorInner.a);

    vec4 bodyColorOuter = vec4(u_TrackColorOuter.rgb, v_Color.a * 0.6);
    vec4 bodyColorInner = vec4(u_TrackColorInner.rgb, v_Color.a * 0.6);

    float borderWidthScaled = u_BorderWidth < 1.0 ? u_BorderWidth * baseBorderWidth : (u_BorderWidth - 1.0) * slope + baseBorderWidth;
    float borderMid = borderStart + borderWidthScaled / 2.0;
    float borderEnd = borderStart + borderWidthScaled;

    vec4 borderColorMix = mix(borderColorOuter, borderColorInner, smoothstep(borderMid - borderWidthScaled/4.0, borderMid + borderWidthScaled/4.0, distance_inv));
    vec4 bodyColorMix = mix(bodyColorOuter, bodyColorInner, (distance_inv - borderEnd) / (1.0 - borderEnd));

    if (u_BorderWidth < 0.01) {
        borderColorMix = outerShadow;
    }
    else if (u_BorderWidth > 9.99) {
        bodyColorMix = borderColorMix;
    }

    if (distance_inv <= borderStart - blend) {
        sliderTexture = outerShadow;
    }
    else if (distance_inv > borderStart-blend && distance_inv < borderStart+blend) {
        sliderTexture = mix(outerShadow, borderColorMix, (distance_inv - (borderStart - blend)) / (2.0 * blend));
    }
    else if (distance_inv > borderStart+blend && distance_inv <= borderEnd-blend) {
        sliderTexture = borderColorMix;
    }
    else if (distance_inv > borderEnd-blend && distance_inv < borderEnd+blend) {
        sliderTexture = mix(borderColorMix, bodyColorMix, (distance_inv - (borderEnd - blend)) / (2.0 * blend));
    }
    else if (distance_inv > borderEnd + blend) {
        sliderTexture = bodyColorMix;
    }

    return sliderTexture;
}


void main() {
    vec4 texColor;
    
    //Programmatically insert the switch statement for choosing the correct sampler
    //Gets inserted when this file is loaded
    //#SWITCH

    //Red color hack to choose a slider lol
    if(v_Color.r < 1000.0)
        FragColor = texColor * v_Color;
    else
	    FragColor = slider(texColor);

    FragColor = (FragColor * u_FinalColorMult) + vec4(u_FinalColorAdd, 0);

    /*
    vec2 Size = vec2(113, 10);
    vec2 pixelPos = vec2(v_TexCoordinate.x * Size.x, v_TexCoordinate.y * Size.y);

    float Radius = 40.0;

    // Calculate distance to edge
    float distance = roundedBoxSDF(pixelPos - (Size / 2.0), Size / 2.0, Radius);

    float distanceVal = 0.01 - distance;

    //discard pixels that are outside our rounded rectangle shape
    if(distanceVal <= 0.0)
        discard;
  
    FragColor.a *= distanceVal < 1.0 ? distanceVal : 1.0;
    */
	//test
}
