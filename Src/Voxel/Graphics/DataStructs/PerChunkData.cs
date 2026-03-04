using System.Diagnostics.CodeAnalysis;
using Engine3.Test.Voxel.World;

namespace Engine3.Test.Voxel.Graphics.DataStructs {
	public readonly record struct PerChunkData {
		public required ChunkPos ChunkPos { get; init; }

		[SetsRequiredMembers] public PerChunkData(ChunkPos chunkPos) => ChunkPos = chunkPos;
	}
}