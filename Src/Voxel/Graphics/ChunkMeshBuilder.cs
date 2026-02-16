using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.Graphics {
	public static class ChunkMeshBuilder { // TODO
		private const byte FaceCount = 6;
		private const byte VerticesPerFace = 4;

		[MustUseReturnValue]
		public static uint[] CreateChunkIndices(Chunk chunk) {
			List<uint> indices = new();

			uint offset = 0;
			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Block b = chunk[i];

				BlockFace blockFaceMask = b.Properties.SolidFaceMask;
				if (blockFaceMask == BlockFace.None) { continue; }

				// TODO check if face is visible

				if (blockFaceMask.HasFlagFast(BlockFace.North)) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFace.East)) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFace.South)) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFace.West)) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFace.Up)) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFace.Down)) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;
			}

			return indices.ToArray();
		}

		[MustUseReturnValue]
		public static ChunkVertex[] GetChunkVertices() {
			const float S = 0.5f;

			ChunkVertex[] vertices = new ChunkVertex[Chunk.ArraySize * FaceCount * VerticesPerFace];

			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Chunk.FromIndex(i, out byte x, out byte y, out byte z);

				uint offset = 0;
				uint faceOffset = (uint)i * FaceCount * VerticesPerFace;

				float x0 = x - S;
				float x1 = x + S;
				float y0 = y - S;
				float y1 = y + S;
				float z0 = z - S;
				float z1 = z + S;

				// north
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();

				// east
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();

				// south
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();

				// west
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();
				vertices[faceOffset + offset++] = new();

				// up
				vertices[faceOffset + offset++] = new(x1, y1, z1);
				vertices[faceOffset + offset++] = new(x0, y1, z1);
				vertices[faceOffset + offset++] = new(x0, y1, z0);
				vertices[faceOffset + offset++] = new(x1, y1, z0);

				// down
				vertices[faceOffset + offset++] = new(x0, y0, z0);
				vertices[faceOffset + offset++] = new(x0, y0, z1);
				vertices[faceOffset + offset++] = new(x1, y0, z1);
				vertices[faceOffset + offset] = new(x1, y0, z0);
			}

			return vertices;
		}
	}
}