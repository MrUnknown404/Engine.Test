using System.Diagnostics.CodeAnalysis;
using Engine3.Test.Voxel.Graphics.DataStructs;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class ChunkOutlineBuffer {
		private static byte MatrixSize { get; } = (byte)sizeof(ChunkOutlineData);

		public required ChunkOutlineData[] Data { get; init; }

		public uint Count { get; }
		public ulong Size { get; }

		[SetsRequiredMembers]
		public ChunkOutlineBuffer(uint count) {
			Data = new ChunkOutlineData[count];
			Count = count;
			Size = MatrixSize * Count;
		}
	}
}