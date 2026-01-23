struct FSInput {
	[[vk::location(0)]]float3 Color: COLOR0;
};

float4 main(FSInput input) {
	return float4(input.Color, 1);
}