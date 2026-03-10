using System.Diagnostics.CodeAnalysis;
using OpenTK.Mathematics;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class ChunkOutlineBuffer {
		private static byte MatrixSize { get; } = (byte)sizeof(Vector3i);

		public required Vector3i[] Positions { get; init; }

		public uint Count { get; }
		public ulong Size { get; }

		[SetsRequiredMembers]
		public ChunkOutlineBuffer(uint count) {
			Positions = new Vector3i[count];
			Count = count;
			Size = MatrixSize * Count;
		}
	}
}