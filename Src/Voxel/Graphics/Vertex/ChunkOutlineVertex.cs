using System.Numerics;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Vertex {
	public readonly unsafe record struct ChunkOutlineVertex {
		public float X { get; init; }
		public float Y { get; init; }
		public float Z { get; init; }

		public ChunkOutlineVertex() { }

		public ChunkOutlineVertex(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		public ChunkOutlineVertex(Vector3 position) : this(position.X, position.Y, position.Z) { }

		public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(ChunkOutlineVertex), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];
		public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [ new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, ];
	}
}