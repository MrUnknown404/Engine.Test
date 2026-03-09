namespace Engine3.Test.Voxel.World {
	public class HeightMap {
		public int GetAt(int x, int z) => x == 0 || z == 0 ? 1 : 0;
	}
}