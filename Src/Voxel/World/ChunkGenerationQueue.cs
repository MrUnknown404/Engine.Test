using Engine3.Test.Voxel.Blocks;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public class ChunkGenerationQueue {
		public uint ChunkCount => (uint)chunksToGenerate.Count;
		public bool ShouldGenerateChunks => ChunkCount != 0;

		private readonly World world;
		private readonly HashSet<ChunkPos> chunksToGenerate = new();

		public ChunkGenerationQueue(World world) => this.world = world;

		public void Enqueue(ChunkPos position) => chunksToGenerate.Add(position);

		[MustUseReturnValue]
		internal Chunk[] GenerateChunks() { // TODO multithread
			Chunk[] chunks = chunksToGenerate.AsValueEnumerable().Select(pos => GenerateChunk(world, pos)).ToArray();
			chunksToGenerate.Clear();
			return chunks;
		}

		[MustUseReturnValue]
		private static Chunk GenerateChunk(World world, ChunkPos chunkPos) {
			Block[] blocks = new Block[Chunk.ArraySize];

			for (byte x = 0; x < Chunk.Size; x++) {
				for (byte z = 0; z < Chunk.Size; z++) {
					int height = world.HeightMap.GetAt(x, z);

					for (byte y = 0; y < Chunk.Size; y++) {
						blocks[Chunk.ToIndex(x, y, z)] = chunkPos.Y * Chunk.Size + y < height ? Block.Stone : Block.Air; //
					}
				}
			}

			return new(world, chunkPos, blocks);
		}
	}
}