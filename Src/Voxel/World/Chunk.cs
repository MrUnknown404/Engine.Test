using Engine3.Test.Voxel.Blocks;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public class Chunk {
		public const byte Size = 16;
		public const ushort ArraySize = Size * Size * Size;

		private readonly World world;

		public ChunkPos ChunkPos { get; }

		private readonly Block[] blocks = new Block[ArraySize];

		public Chunk(World world, ChunkPos chunkPos) {
			this.world = world;
			ChunkPos = chunkPos;

			Array.Fill(blocks, Block.Stone);
		}

		internal Block this[ushort index] {
			get => blocks[index];
			private set {
				blocks[index] = value;
				world.MarkChunkDirty(this);
			}
		}

		public Block this[byte x, byte y, byte z] { get => this[ToIndex(x, y, z)]; set => this[ToIndex(x, y, z)] = value; }

		[MustUseReturnValue] private static ushort ToIndex(byte x, byte y, byte z) => (ushort)(x + y * Size * Size + z * Size);

		internal static void FromIndex(ushort index, out byte x, out byte y, out byte z) {
			x = (byte)(index % Size);
			y = (byte)(index / (Size * Size));
			z = (byte)(index / Size % Size);
		}
	}
}