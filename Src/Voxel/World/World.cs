using System.Diagnostics.CodeAnalysis;
using NLog;

namespace Engine3.Test.Voxel.World {
	public class World {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		// TODO how do i want to store chunks? do i want to separate chunks by how they're used? rendering (client only), updates, etc?

		private readonly Dictionary<ChunkPos, Chunk> chunks = new();

		private readonly Queue<ChunkPos> rendererDirtyChunks = new();
		internal bool HasDirtyChunksForRenderer => rendererDirtyChunks.Count != 0;

		public World() {
			const int S = 3;

			for (int x = 0; x < S; x++) {
				for (int y = 0; y < S; y++) {
					for (int z = 0; z < S; z++) { AddChunk(new(x, y, z)); }
				}
			}

			// ChunkPos chunkPos = new();
			// chunks[chunkPos][0, 0, 0] = Block.Air;
			// chunks[chunkPos][1, 0, 0] = Block.Air;

			// chunkPos = new() { Y = 1, };
			// chunks[chunkPos][0, 15, 0] = Block.Air;
		}

		public bool TryGetChunk(ChunkPos position, [NotNullWhen(true)] out Chunk? chunk) => chunks.TryGetValue(position, out chunk);

		// TODO think about how i want to do chunk generation. probably should make a generation queue?
		private void AddChunk(ChunkPos position) {
			if (chunks.ContainsKey(position)) {
				Logger.Warn($"Attempted to add chunk at {position} but we already have a chunk there!");
				return;
			}

			Chunk chunk = new(this, position);
			chunks.Add(position, chunk);
			MarkChunkDirty(position);
		}

		public ChunkPos[] CleanRendererDirtyChunks() {
			if (!HasDirtyChunksForRenderer) { return Array.Empty<ChunkPos>(); }

			ChunkPos[] positions = rendererDirtyChunks.AsValueEnumerable().Distinct().ToArray();
			rendererDirtyChunks.Clear();
			return positions;
		}

		public void MarkChunkDirty(ChunkPos position) => rendererDirtyChunks.Enqueue(position);
	}
}