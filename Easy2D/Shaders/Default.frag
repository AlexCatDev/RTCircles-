#version 300 es
//I really have no fucking clue what precision should be and how it matters so...
precision mediump float;

uniform sampler2D u_Textures[16];

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

    vec3 lightcolor = vec3(1.0);

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

    vec4 bodyColorOuter = vec4(u_TrackColorOuter.rgb, v_Color.a * 0.5);
    vec4 bodyColorInner = vec4(u_TrackColorInner.rgb, v_Color.a * 0.5);

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
    vec4 texColor = vec4(1.0);
    //texColor = texture(u_Textures[v_TextureSlot], v_TexCoordinate) * v_Color;
    //Add_Switch_Statement^
    switch(v_TextureSlot) {
        case 0:
        texColor = texture(u_Textures[0], v_TexCoordinate);
        break;
        case 1:
        texColor = texture(u_Textures[1], v_TexCoordinate);
        break;
        case 2:
        texColor = texture(u_Textures[2], v_TexCoordinate);
        break;
        case 3:
        texColor = texture(u_Textures[3], v_TexCoordinate);
        break;
        case 4:
        texColor = texture(u_Textures[4], v_TexCoordinate);
        break;
        case 5:
        texColor = texture(u_Textures[5], v_TexCoordinate);
        break;
        case 6:
        texColor = texture(u_Textures[6], v_TexCoordinate);
        break;
        case 7:
        texColor = texture(u_Textures[7], v_TexCoordinate);
        break;
        case 8:
        texColor = texture(u_Textures[8], v_TexCoordinate);
        break;
        case 9:
        texColor = texture(u_Textures[9], v_TexCoordinate);
        break;
        case 10:
        texColor = texture(u_Textures[10], v_TexCoordinate);
        break;
        case 11:
        texColor = texture(u_Textures[11], v_TexCoordinate);
        break;
        case 12:
        texColor = texture(u_Textures[12], v_TexCoordinate);
        break;
        case 13:
        texColor = texture(u_Textures[13], v_TexCoordinate);
        break;
        case 14:
        texColor = texture(u_Textures[14], v_TexCoordinate);
        break;
        case 15:
        texColor = texture(u_Textures[15], v_TexCoordinate);
        break;
    }

    //Red color hack to choose a slider lol
    if(v_Color.r < 1000.0)
        FragColor = texColor * v_Color;
    else
	    FragColor = slider(texColor);

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
