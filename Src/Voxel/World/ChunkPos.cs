namespace Engine3.Test.Voxel.World {
	public readonly record struct ChunkPos {
		public int X { get; init; }
		public int Y { get; init; }
		public int Z { get; init; }

		public ChunkPos(int x, int y, int z) {
			X = x;
			Y = y;
			Z = z;
		}
	}
}