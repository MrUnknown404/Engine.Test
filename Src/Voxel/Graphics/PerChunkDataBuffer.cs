using System.Diagnostics.CodeAnalysis;
using Engine3.Test.Voxel.Graphics.DataStructs;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class PerChunkDataBuffer {
		private static byte DataSize { get; } = (byte)sizeof(PerChunkData);

		public required PerChunkData[] Data { get; init; }

		public uint Count { get; }
		public uint Size { get; }

		[SetsRequiredMembers]
		public PerChunkDataBuffer(PerChunkData[] data) {
			Data = data;
			Count = (uint)data.Length;
			Size = DataSize * Count;
		}

		[SetsRequiredMembers]
		public PerChunkDataBuffer(uint count) {
			Data = new PerChunkData[count];
			Count = count;
			Size = DataSize * Count;
		}
	}
}