namespace Engine3.Test.Voxel.World {
	public class HeightMap {
		private readonly FastNoiseLite noiseProvider = new();

		public HeightMap(WorldProperties worldProperties) {
			noiseProvider.SetSeed(worldProperties.Seed);
			noiseProvider.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		}

		public int GetBlockHeightAt(int x, int z) { // TODO cache
			const float Scale = Chunk.Size * 2;

			float noise = (noiseProvider.GetNoise(x, z) + 1) / 2; // GetNoise is normally [-1 .. +1]
			return (int)(noise * Scale) - Chunk.Size;
		}

		public int GetChunkHeightAt(int chunkX, int chunkZ) { // TODO cache. also probably do something better. not sure yet. do avg? highest? lowest? all? something else?
			int highest = 0;

			for (int x = 0; x < Chunk.Size; x++) {
				for (int z = 0; z < Chunk.Size; z++) {
					int height = GetBlockHeightAt(chunkX + x, chunkZ + z);
					if (height > highest) { highest = height; }
				}
			}

			return highest;
		}
	}
}