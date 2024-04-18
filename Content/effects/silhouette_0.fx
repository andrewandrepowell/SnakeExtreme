
Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

static const float4 Transparent = float4(0, 0, 0, 0);
float4 OverlayColor;

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    if (color.a > 0)
        return OverlayColor * input.Color;
    return Transparent;
}

technique BasicColorDrawing
{
	pass pass0
	{
		PixelShader = compile ps_4_0_level_9_1 MainPS();
	}
};