using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Graphics.Test {
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public readonly record struct TestVertex2 {
		public required float X { get; init; }
		public required float Y { get; init; }
		public required float Z { get; init; }

		public required float U { get; init; }
		public required float V { get; init; }

		public required float R { get; init; }
		public required float G { get; init; }
		public required float B { get; init; }

		public TestVertex2() { }

		[SetsRequiredMembers]
		public TestVertex2(float x, float y, float z, float u, float v, float r, float g, float b) {
			X = x;
			Y = y;
			Z = z;
			U = u;
			V = v;
			R = r;
			G = g;
			B = b;
		}

		public static unsafe VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(TestVertex2), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
				new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
				new() { binding = binding, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 3, }, //
				new() { binding = binding, location = 2, format = VkFormat.FormatR32g32b32Sfloat, offset = sizeof(float) * 5, },
		];
	}
}