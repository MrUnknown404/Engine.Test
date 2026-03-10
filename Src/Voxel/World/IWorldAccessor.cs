using System.Diagnostics.CodeAnalysis;

namespace Engine3.Test.Voxel.World {
	public interface IWorldAccessor {
		public WorldProperties WorldProperties { get; }
		public HeightMap HeightMap { get; }

		public bool TryGetChunk(ChunkPos position, [NotNullWhen(true)] out IChunkAccessor? chunkAccessor);
	}
}