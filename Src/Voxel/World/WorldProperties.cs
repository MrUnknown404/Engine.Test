using System.Diagnostics.CodeAnalysis;

namespace Engine3.Test.Voxel.World {
	public class WorldProperties {
		public required int Seed { get; init; } // TODO ulong. Random takes int. use something else?

		// bounds
		public uint Width { get; init; } = Chunk.Size * ushort.MaxValue;
		public ushort Depth { get; init; } = Chunk.Size * 64;
		public ushort Height { get; init; } = Chunk.Size * 64;

		// misc
		public int SeaLevel { get; init; }

		public WorldProperties() { }

		[SetsRequiredMembers] public WorldProperties(int seed) => Seed = seed;
	}
}