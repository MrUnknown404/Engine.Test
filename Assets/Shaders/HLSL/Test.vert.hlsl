struct VSInput {
	[[vk::location(0)]]float2 Position: POSITION0;
	[[vk::location(1)]]float3 Color: COLOR0;
};

struct VSOutput {
	float4 Position: SV_POSITION;
	[[vk::location(0)]]float3 Color: COLOR0;
};

VSOutput main(VSInput input) {
	VSOutput output;
	output.Position = float4(input.Position, 0, 1);
	output.Color = input.Color;
	return output;
}