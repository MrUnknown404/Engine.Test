using Engine3.Test.Voxel.Graphics;

namespace Engine3.Test.Voxel.World {
	public class World {
		public Chunk Chunk { get; }

		private readonly VoxelRenderer renderer;

		public World(VoxelRenderer renderer) {
			this.renderer = renderer;
			Chunk = new(this, new());
			MarkChunkDirty(Chunk);
		}

		public void MarkChunkDirty(Chunk chunk) {
			renderer.MarkChunkDirty(); // more?
		}
	}
}