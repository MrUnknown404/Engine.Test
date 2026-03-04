namespace Engine3.Test.Voxel.World {
	public readonly record struct LocalBlockPos {
		public byte X { get; init; }
		public byte Y { get; init; }
		public byte Z { get; init; }

		public LocalBlockPos(byte x, byte y, byte z) {
			X = x;
			Y = y;
			Z = z;
		}
	}
}