using System.Diagnostics.CodeAnalysis;

namespace Engine3.Test.Voxel.World {
	public interface IWorldWriter {
		public bool TryEditChunk(ChunkPos position, [NotNullWhen(true)] out IChunkWriter? chunkWriter);
	}
}