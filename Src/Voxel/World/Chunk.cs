using Engine3.Test.Voxel.Blocks;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.World {
	public class Chunk {
		public const byte Size = 16;
		public const ushort ArraySize = Size * Size * Size;

		private readonly World world;

		public ChunkPos Position { get; }

		private readonly Block[] blocks = new Block[ArraySize];

		internal Chunk(World world, ChunkPos position, Block[]? blocks) {
			this.world = world;
			Position = position;

			if (blocks == null) { Array.Fill(this.blocks, Block.Air); } else { this.blocks = blocks; }
		}

		internal Block this[ushort index] {
			[MustUseReturnValue] get => blocks[index];
			private set {
				blocks[index] = value;
				world.MarkChunkDirty(Position);
			}
		}

		[Obsolete] // TODO make system for editing large amounts of blocks at once & force that (maybe?)
		public Block this[byte x, byte y, byte z] {
			[MustUseReturnValue] get => this[ToIndex(x, y, z)];
			set => this[ToIndex(x, y, z)] = value;
		}

		[Obsolete] // TODO make system for editing large amounts of blocks at once & force that (maybe?)
		public Block this[LocalBlockPos blockPos] {
			[MustUseReturnValue] get => this[ToIndex(blockPos)];
			set => this[ToIndex(blockPos)] = value;
		}

		[MustUseReturnValue] internal static ushort ToIndex(byte x, byte y, byte z) => (ushort)(x + y * Size * Size + z * Size);
		[MustUseReturnValue] internal static ushort ToIndex(LocalBlockPos blockPos) => (ushort)(blockPos.X + blockPos.Y * Size * Size + blockPos.Z * Size);

		internal static void FromIndex(ushort index, out byte x, out byte y, out byte z) {
			x = (byte)(index % Size);
			y = (byte)(index / (Size * Size));
			z = (byte)(index / Size % Size);
		}

		[MustUseReturnValue] internal static LocalBlockPos FromIndex(ushort index) => new((byte)(index % Size), (byte)(index / (Size * Size)), (byte)(index / Size % Size));
	}
}