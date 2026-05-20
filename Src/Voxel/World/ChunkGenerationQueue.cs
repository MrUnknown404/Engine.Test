using Engine3.Test.Voxel.Blocks;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World;

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
		if (chunkPos.Y > 10) { return new(world, chunkPos); } // skip if we know it'll be air only. remove/update later

		BlockState[] blocks = new BlockState[Chunk.ArraySize];

		bool isEmpty = true;

		int chunkXOffset = chunkPos.X * Chunk.Size;
		int chunkYOffset = chunkPos.Y * Chunk.Size;
		int chunkZOffset = chunkPos.Z * Chunk.Size;

		Random random = new(); // TODO remove

		for (byte x = 0; x < Chunk.Size; x++) {
			int newX = chunkXOffset + x;

			for (byte z = 0; z < Chunk.Size; z++) {
				int newZ = chunkZOffset + z;
				int height = world.HeightMap.GetBlockHeightAt(newX, newZ);

				for (byte y = 0; y < Chunk.Size; y++) {
					int newY = chunkYOffset + y;

					Block block = Blocks.Blocks.Air;
					if (newY < height) {
						block = random.Next(3) switch {
								0 => Blocks.Blocks.Stone,
								1 => Blocks.Blocks.Dirt,
								2 => Blocks.Blocks.Grass,
								_ => throw new ArgumentOutOfRangeException(),
						};

						isEmpty = false;
					}

					blocks[Chunk.ToIndex(x, y, z)] = new(block, BlockStateFlags.WasGenerated);
				}
			}
		}

		return new(world, chunkPos, blocks, isEmpty);
	}
}