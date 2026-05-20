using System.Numerics;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Vertex;

public readonly unsafe record struct ChunkOutlineVertex {
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }

	public uint Color { get; init; }

	public ChunkOutlineVertex() { }

	public ChunkOutlineVertex(float x, float y, float z, uint color) {
		X = x;
		Y = y;
		Z = z;
		Color = color;
	}

	public ChunkOutlineVertex(Vector3 position, uint color) : this(position.X, position.Y, position.Z, color) { }

	public static VkVertexInputBindingDescription[] GetBindingDescriptions(uint binding = 0) => [ new() { binding = binding, stride = (uint)sizeof(ChunkOutlineVertex), inputRate = VkVertexInputRate.VertexInputRateVertex, }, ];

	public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(uint binding = 0) => [
			new() { binding = binding, location = 0, format = VkFormat.FormatR32g32b32Sfloat, offset = 0, }, //
			new() { binding = binding, location = 1, format = VkFormat.FormatR32Uint, offset = sizeof(float) * 3, }, //
	];
}