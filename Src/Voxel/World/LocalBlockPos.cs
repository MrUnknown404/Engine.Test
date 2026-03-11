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

		public LocalBlockPos(GlobalBlockPos globalBlockPos) {
			int modX = globalBlockPos.X % Chunk.Size;
			int modY = globalBlockPos.Y % Chunk.Size;
			int modZ = globalBlockPos.Z % Chunk.Size;

			X = (byte)(globalBlockPos.X < 0 && modX != 0 ? Chunk.Size + modX : modX);
			Y = (byte)(globalBlockPos.Y < 0 && modY != 0 ? Chunk.Size + modY : modY);
			Z = (byte)(globalBlockPos.Z < 0 && modZ != 0 ? Chunk.Size + modZ : modZ);
		}
	}
}