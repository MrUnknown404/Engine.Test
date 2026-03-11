using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;

namespace Engine3.Test.Voxel.Graphics.DataStructs {
	public readonly record struct ChunkOutlineData {
		public required Vector3i Position { get; init; }
		public required uint Color { get; init; }

		[SetsRequiredMembers]
		public ChunkOutlineData(Vector3i position, uint color) {
			Position = position;
			Color = color;
		}
	}
}