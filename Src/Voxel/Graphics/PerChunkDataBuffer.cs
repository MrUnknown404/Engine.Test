using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class PerChunkDataBuffer {
		private static byte MatrixSize { get; } = (byte)sizeof(Matrix4x4);

		public required ChunkData[] Data { get; init; }

		public ulong Count { get; }
		public ulong Size { get; }

		[SetsRequiredMembers]
		public PerChunkDataBuffer(ulong count) {
			Data = new ChunkData[count];
			Count = count;
			Size = MatrixSize * Count;
		}

		public readonly record struct ChunkData {
			public ChunkPos ChunkPos { get; }

			public ChunkData(ChunkPos chunkPos) => ChunkPos = chunkPos;
		}
	}
}