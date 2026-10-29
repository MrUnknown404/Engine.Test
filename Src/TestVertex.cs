using Engine4.Client.Graphics.Data;
using OpenTK.Graphics.Vulkan;

namespace Engine4.Test;

public readonly record struct TestVertex : IVertex {
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }

	public float U { get; init; }
	public float V { get; init; }

	public float R { get; init; }
	public float G { get; init; }
	public float B { get; init; }

	public TestVertex(float x, float y, float z, float u, float v, float r, float g, float b) {
		X = x;
		Y = y;
		Z = z;
		U = u;
		V = v;
		R = r;
		G = g;
		B = b;
	}

	public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding) => IVertex.CreateBindingDescriptions<TestVertex>(binding, VkVertexInputRate.VertexInputRateVertex);

	public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding) =>
			IVertex.CreateAttributeDescriptions(binding, [
					(VkFormat.FormatR32g32b32Sfloat, sizeof(float) * 3), //
					(VkFormat.FormatR32g32Sfloat, sizeof(float) * 2), //
					(VkFormat.FormatR32g32b32Sfloat, sizeof(float) * 3),
			]);
}