namespace Engine3.Test.Voxel.World {
	public class HeightMap {
		private readonly FastNoiseLite noiseProvider = new();

		public HeightMap(WorldProperties worldProperties) {
			noiseProvider.SetSeed(worldProperties.Seed);
			noiseProvider.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		}

		public int GetAt(int x, int z) {
			const float Scale = Chunk.Size;

			float noise = (noiseProvider.GetNoise(x, z) + 1) / 2f; // GetNoise is normally [-1 .. +1]
			return (int)(noise * Scale);
		}
	}
}