float4x4 World; // world matrix.
float4x4 View; // the camera-view matrix.
float4x4 Projection; // the camera-projection.
float3 CameraPosition; // the camera-position.

float4 HorizonColor; // Horizon color used for fogging.
float4 SunColor;

float FogNear; // Near fog plane.
float FogFar; // Far fog plane.

Texture BlockTextureAtlas;
sampler BlockTextureAtlasSampler = sampler_state
{
	texture = <BlockTextureAtlas>;
	magfilter = point; // filter for objects smaller than actual.
	minfilter = point; // filter for objects larger than actual.
	mipfilter = point; // filter for resizing the image up close and far away.
	AddressU = WRAP;
	AddressV = WRAP;
};

struct VertexShaderInput
{
	float4 Position				: SV_POSITION;
	float2 blockTextureCoord	: TEXCOORD0;	// block texture uv-mapping coordinates.
	float4 Light				: COLOR0;		// Light (sun, r, g, b)
};

struct VertexShaderOutput
{
	float4 Position				: SV_POSITION;
    float2 blockTextureCoord	: TEXCOORD0;
    float3 CameraView			: TEXCOORD1;
    float Distance				: TEXCOORD2;
	float4 Color				: COLOR0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);

    output.Position = mul(viewPosition, Projection);
    output.CameraView = normalize(CameraPosition - worldPosition);
    output.Distance = length(CameraPosition - worldPosition);

    output.blockTextureCoord = input.blockTextureCoord;

	output.Color.rgb = clamp(input.Light.x * SunColor + input.Light.yzw, 0, 1);
	output.Color.a = 1;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 blockTextureColor = tex2D(BlockTextureAtlasSampler, input.blockTextureCoord);
    float fog = saturate((input.Distance - FogNear) / (FogNear-FogFar));    

    float4 color;
	color.rgb  = blockTextureColor.rgb * input.Color.rgb;
	color.a = blockTextureColor.a;
    if(color.a == 0) { clip(-1); }

	float4 sunColor = SunColor;	 
	float4 fogColor = HorizonColor;

	return lerp(fogColor, color ,fog);
}

technique BlockTechnique
{
    pass Pass1
    {
        VertexShader = compile vs_4_0 VertexShaderFunction();
        PixelShader = compile ps_4_0 PixelShaderFunction();
    }
}
