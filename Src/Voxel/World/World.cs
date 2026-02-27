using Engine3.Test.Voxel.Graphics;

namespace Engine3.Test.Voxel.World {
	public class World {
		public Chunk Chunk { get; }

		private readonly VoxelRenderPassRenderer renderer;

		public World(VoxelRenderPassRenderer renderer) {
			this.renderer = renderer;
			Chunk = new(this, new());
			MarkChunkDirty(Chunk);
		}

		public void MarkChunkDirty(Chunk chunk) {
			renderer.MarkChunkDirty(); // more?
		}
	}
}