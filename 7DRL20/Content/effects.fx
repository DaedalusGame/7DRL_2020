#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

static const float PI = 3.14159265f;
static const float TAU = 6.28318530f;
static const float NOISE_TEXTURE_SIZE = 256.0;

matrix WorldViewProjection;

texture2D texture_main : register(t0);
texture2D texture_map : register(t1);
SamplerState sampler_main;

float4x4 map_transform;

float4 gradient_topleft;
float4 gradient_topright;
float4 gradient_bottomleft;
float4 gradient_bottomright;

float angle_target;
float angle_spread;

float4x4 color_matrix;
float4 color_add;

float2 distort_offset;

float threshold;
float4x4 threshold_color_matrix;
float4 threshold_color_add;

//Wave shader
float wave_time_horizontal, wave_time_vertical;
float wave_distance_horizontal, wave_distance_vertical;
float wave_split_horizontal, wave_split_vertical;
float4 wave_components;

//Glitch shader (by Jan Vorisek @blokatt)
float glitch_intensity;
float glitch_time;
float2 glitch_resolution;
float glitch_rng_seed;
float3 glitch_random_values;
texture2D glitch_noise_texture : register(t1);

float glitch_line_speed;
float glitch_line_drift;
float glitch_line_resolution;
float glitch_line_vert_shift;
float glitch_line_shift;
float glitch_jumbleness;
float glitch_jumble_resolution;
float glitch_jumble_shift;
float glitch_jumble_speed;
float glitch_dispersion;
float glitch_channel_shift;
float glitch_noise_level;
float glitch_shakiness;

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 ScreenCoords : TEXCOORD1;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 extractRed(float4 col)
{
	return float4(col.r, 0., 0., col.a);
}

float4 extractGreen(float4 col)
{
	return float4(0., col.g, 0., col.a);
}

float4 extractBlue(float4 col)
{
	return float4(0., 0., col.b, col.a);
}

float fract(float x)
{
	return x - floor(x);
}

float2 mirror(float2 v)
{
	return abs((frac((v * 0.5) + 0.5) * 2.0) - 1.0);
}

float2 downsample(float2 v, float2 res)
{
	return floor(v * res) / res;
}

float4 whiteNoise(float2 coord, float2 texelOffset)
{
	float2 offset = downsample(float2(glitch_rng_seed * NOISE_TEXTURE_SIZE, glitch_rng_seed) + texelOffset, float2(NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE));
	float2 halfTexelSize = (float2(1.0, 1.0) / (float2(NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE) * 2.0));
	float2 ratio = glitch_resolution / float2(NOISE_TEXTURE_SIZE, NOISE_TEXTURE_SIZE);
	return glitch_noise_texture.Sample(sampler_main, (coord * ratio) + offset);
}

float mod(float x, float y)
{
	return x - y * floor(x / y);
}

float2 jumble(float2 coord, float res)
{
	float2 gridCoords = (coord * res) / (NOISE_TEXTURE_SIZE * 0.0245);
	float4 cellRandomValues = whiteNoise(gridCoords, float2(res, res) * -10.0 + (2.5 * float2(-0.75, 0.25)) * mod(floor(glitch_time * 0.02 * glitch_jumble_speed), 1000.0));
	return (cellRandomValues.ra - 0.5) * glitch_jumble_shift * floor(min(0.99999, cellRandomValues.b) + glitch_jumbleness);
}

float lineOffset(float2 coord, float2 texcoord)
{
	float2 waveHeights = float2(50.0 * glitch_line_resolution, 25.0 * glitch_line_resolution);
	float4 lineRandom = whiteNoise(downsample(texcoord.yy, waveHeights), float2(0, 0));
	float driftTime = texcoord.y * glitch_resolution.y * 2.778;
    
	float4 waveTimes = (float4(downsample(lineRandom.ra * TAU, waveHeights) * 80.0, driftTime + 2.0, (driftTime * 0.1) + 1.0) + (glitch_time * glitch_line_speed)) + (glitch_line_vert_shift * TAU);
	float4 waveLineOffsets = float4(sin(waveTimes.x), cos(waveTimes.y), sin(waveTimes.z), cos(waveTimes.w));
	waveLineOffsets.xy *= ((whiteNoise(waveTimes.xy, float2(0, 0)).gb - 0.5) * glitch_shakiness) + 1.0;
	waveLineOffsets.zw *= glitch_line_drift;
	return dot(waveLineOffsets, float4(1, 1, 1, 1));
}

float2 transformTexCoord(float4x4 x, float2 texcoord)
{
	return mul(x, float4(texcoord.x,texcoord.y,0,0)).xy;
}

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
	output.ScreenCoords = input.Position.xy;
	
	return output;
}

VertexShaderOutput WaveVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput) 0;

	float4 projected = mul(input.Position, WorldViewProjection);
	output.Position = projected;
	output.Color = input.Color;
	output.TextureCoordinates = input.TextureCoordinates;
	output.ScreenCoords = input.Position.xy;

	return output;
}

float4 WavePS(VertexShaderOutput input) : COLOR
{
	float x = input.ScreenCoords.x > wave_split_horizontal ? input.ScreenCoords.x : 2 * wave_split_horizontal - input.ScreenCoords.x;
	float y = input.ScreenCoords.y > wave_split_vertical ? input.ScreenCoords.y : 2 * wave_split_vertical - input.ScreenCoords.y;
	float4 components = float4(y, x, x, y) * wave_components;
	return texture_main.Sample(sampler_main, input.TextureCoordinates + float2(sin(wave_time_horizontal + components.x + components.y) * 0.5 * wave_distance_horizontal, sin(wave_time_vertical + components.z + components.w) * 0.5 * wave_distance_vertical)) * input.Color;
}

float4 GradientPS(VertexShaderOutput input) : COLOR
{
    float4 color = lerp(lerp(gradient_topleft, gradient_topright, input.ScreenCoords.xxxx), lerp(gradient_bottomleft, gradient_bottomright, input.ScreenCoords.xxxx), input.ScreenCoords.yyyy);
	return input.Color * color;
}

float4 ColorMatrixPS(VertexShaderOutput input) : COLOR
{
	float4 color = texture_main.Sample(sampler_main, input.TextureCoordinates) * input.Color;
	if (color.a <= 0)
		return color;
	return mul(color_matrix, color) + color_add;
}

float4 ColorMatrixThresholdPS(VertexShaderOutput input) : COLOR
{
	float4 color = texture_main.Sample(sampler_main, input.TextureCoordinates) * input.Color;
	float4 mask = texture_map.Sample(sampler_main, transformTexCoord(map_transform, input.TextureCoordinates));
	if (color.a <= 0)
		return color;
	if (mask.r < threshold)
		return mul(threshold_color_matrix, color) + threshold_color_add;
	else
		return mul(color_matrix, color) + color_add;
}

float4 ClockPS(VertexShaderOutput input) : COLOR
{
	float4 color = texture_main.Sample(sampler_main, input.TextureCoordinates) * input.Color;
	float dx = input.TextureCoordinates.x - 0.5;
	float dy = input.TextureCoordinates.y - 0.5;
	float angle = atan2(dy, dx);
	float da = (angle - angle_target) % TAU;
	float anglediff = abs((2 * da) % TAU - da);
	if (anglediff > angle_spread)
		return float4(0, 0, 0, 0);
	return color;
}

float4 DistortPS(VertexShaderOutput input) : COLOR
{
	float4 mask = texture_map.Sample(sampler_main, transformTexCoord(map_transform, input.TextureCoordinates));
	float2 offset = (mask.r - 0.5) * 2 * distort_offset * mask.g;
	float4 color = texture_main.Sample(sampler_main, input.TextureCoordinates + offset) * input.Color;
	return color;
}

float4 CirclePS(VertexShaderOutput input) : COLOR
{
	float dx = input.TextureCoordinates.x - 0.5;
	float dy = input.TextureCoordinates.y - 0.5;
	float angle = atan2(dy, dx);
	float dist = sqrt(dx * dx + dy * dy) * 2;
	float4 color = texture_main.Sample(sampler_main, transformTexCoord(map_transform, float2(dist, angle / TAU + 0.5))) * input.Color;
	if (dist > 1)
		return float4(0, 0, 0, 0);
	return color;
}

float4 GlitchPS(VertexShaderOutput input) : COLOR
{
	float4 randomHiFreq = whiteNoise(input.TextureCoordinates, glitch_random_values.xy);
    
	float2 offsetCoords = input.TextureCoordinates;
	offsetCoords.x += ((((2.0 * glitch_random_values.z) - 1.0) * glitch_shakiness * glitch_line_speed) + lineOffset(offsetCoords, input.TextureCoordinates)) * glitch_line_shift * glitch_intensity;
    
	offsetCoords += jumble(offsetCoords, glitch_jumble_resolution) * glitch_intensity * glitch_intensity * 0.25;
        
	float2 shiftFactors = (glitch_channel_shift + (randomHiFreq.rg * glitch_dispersion)) * glitch_intensity;
	float4 outColour = extractRed(texture_main.Sample(sampler_main, mirror(offsetCoords + float2(shiftFactors.r, 0.0))))
	+ extractBlue(texture_main.Sample(sampler_main, mirror(offsetCoords + float2(-shiftFactors.g, 0.0))))
	+ extractGreen(texture_main.Sample(sampler_main, mirror(offsetCoords)));
    
	outColour.rgb *= (float3(.55, .5, .4) * randomHiFreq.gab * glitch_intensity * glitch_noise_level) + 1.0;
        
	return input.Color * outColour;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	return input.Color;
}

technique Wave
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL WaveVS();
		PixelShader = compile PS_SHADERMODEL WavePS();
	}
};
technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
technique Gradient
{
    pass P0
    {
        //VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL GradientPS();
    }
};
technique ColorMatrix
{
	pass P0
	{
        VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL ColorMatrixPS();
	}
};
technique ColorMatrixThreshold
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL ColorMatrixThresholdPS();
	}
};
technique Circle
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL CirclePS();
	}
};
technique Clock
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL ClockPS();
	}
};
technique Distort
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL DistortPS();
	}
};
technique Glitch
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL GlitchPS();
	}
};