using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Graphics.Test {
	public readonly unsafe record struct TestVertex {
		public required float X { get; init; }
		public required float Y { get; init; }
		public required float Z { get; init; }

		public required float R { get; init; }
		public required float G { get; init; }
		public required float B { get; init; }

		public TestVertex() { }

		[SetsRequiredMembers]
		public TestVertex(float x, float y, float z, float r, float g, float b) {
			X = x;
			Y = y;
			Z = z;
			R = r;
			G = g;
			B = b;
		}

		public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(TestVertex), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
				new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
				new() { binding = binding, location = 1, format = VkFormat.FormatR32g32b32Sfloat, offset = sizeof(float) * 3, },
		];
	}
}