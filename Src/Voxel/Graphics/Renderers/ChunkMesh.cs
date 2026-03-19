using Engine3.Test.Voxel.Graphics.Vertex;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public class ChunkMesh {
		public static ChunkMesh EmptyMesh { get; } = new(Array.Empty<ChunkVertex>(), Array.Empty<uint>()) { IsEmpty = true, };

		public ChunkVertex[] Vertices { get; }
		public uint[] Indices { get; }

		public bool IsEmpty { get; private init; }

		public ChunkMesh(ChunkVertex[] vertices, uint[] indices) {
			Vertices = vertices;
			Indices = indices;
		}
	}
}