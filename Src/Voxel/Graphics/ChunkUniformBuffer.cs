using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class ChunkUniformBuffer {
		private static byte MatrixSize { get; } = (byte)sizeof(Matrix4x4);

		public required ChunkPos[] Models { get; init; }
		public ulong Count { get; }
		public ulong Size { get; }

		[SetsRequiredMembers]
		public ChunkUniformBuffer(ulong count) {
			Models = new ChunkPos[count];
			Count = count;
			Size = MatrixSize * Count;
		}
	}
}