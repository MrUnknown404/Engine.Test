using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics {
	public readonly unsafe record struct ChunkVertex {
		public required float X { get; init; }
		public required float Y { get; init; }
		public required float Z { get; init; }

		[SetsRequiredMembers]
		public ChunkVertex() {
			X = 0;
			Y = 0;
			Z = 0;
		}

		[SetsRequiredMembers]
		public ChunkVertex(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(ChunkVertex), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];
		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [ new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, ];

		public override string ToString() => $"({X}, {Y}, {Z})";
	}
}