#version 300 es
//I really have no fucking clue what precision should be and how it matters so...
#if GL_ES
precision mediump float;
#endif

uniform sampler2D u_Texture;

uniform float u_Directions;
uniform float u_Quality;
uniform float u_Radius;

in vec2 v_TexCoordinate;
out vec4 FragColor;

void main() {
	float Pi = 6.28318530718; // Pi*2
    
    // GAUSSIAN BLUR SETTINGS {{{
    //float Directions = 16.0; // BLUR DIRECTIONS (Default 16.0 - More is better but slower)
    //float Quality = 4.0; // BLUR QUALITY (Default 4.0 - More is better but slower)
    //float Size = 0.01; // BLUR SIZE (Radius)
    // GAUSSIAN BLUR SETTINGS }}}
   
    // Pixel colour
    vec4 Color = texture(u_Texture, v_TexCoordinate);
    
    // Blur calculations
    for( float d=0.0; d<Pi; d+=Pi/u_Directions)
    {
		for(float i=1.0/u_Quality; i<=1.0; i+=1.0/u_Quality)
        {
			Color += texture(u_Texture, v_TexCoordinate+vec2(cos(d),sin(d))*u_Radius*i);		
        }
    }
    
    // Output to screen
    Color /= u_Quality * u_Directions;
    FragColor = Color;
}
