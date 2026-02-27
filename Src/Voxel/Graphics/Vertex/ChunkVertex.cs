using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Vertex {
	public readonly unsafe record struct ChunkVertex {
		public required float X { get; init; }
		public required float Y { get; init; }
		public required float Z { get; init; }

		public required float U { get; init; }
		public required float V { get; init; }

		[SetsRequiredMembers]
		public ChunkVertex() {
			X = 0;
			Y = 0;
			Z = 0;
			U = 0;
			V = 0;
		}

		[SetsRequiredMembers]
		public ChunkVertex(float x, float y, float z, float u, float v) {
			X = x;
			Y = y;
			Z = z;
			U = u;
			V = v;
		}

		public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(ChunkVertex), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
				new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
				new() { binding = binding, location = 1, format = VkFormat.FormatR32g32Sfloat, offset = sizeof(float) * 3, },
		];

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}