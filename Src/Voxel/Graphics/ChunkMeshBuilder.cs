using Engine3.Client.Graphics.Vertex;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.Graphics {
	public static class ChunkMeshBuilder {
		private const byte FaceCount = 6;
		private const byte VerticesPerFace = 4;

		[MustUseReturnValue]
		public static uint[] CreateChunkIndices(Chunk chunk) { // TODO optimize. eventually use a compute shader?
			const bool ShowBorder = true;
			const byte ChunkSize = Chunk.Size - 1;

			List<uint> indices = new();

			uint offset = 0;
			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Block b = chunk[i];

				BlockFaceMask blockFaceMask = b.Properties.SolidFaceMask;
				if (blockFaceMask == BlockFaceMask.None) {
					offset += VerticesPerFace * FaceCount;
					continue;
				}

				Chunk.FromIndex(i, out byte x, out byte y, out byte z);
				bool isNorthVisible = z == 0 ? ShowBorder : !chunk[x, y, (byte)(z - 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South);
				bool isEastVisible = x == ChunkSize ? ShowBorder : !chunk[(byte)(x + 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West);
				bool isSouthVisible = z == ChunkSize ? ShowBorder : !chunk[x, y, (byte)(z + 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North);
				bool isWestVisible = x == 0 ? ShowBorder : !chunk[(byte)(x - 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East);
				bool isUpVisible = y == ChunkSize ? ShowBorder : !chunk[x, (byte)(y + 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down);
				bool isDownVisible = y == 0 ? ShowBorder : !chunk[x, (byte)(y - 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up);

				if (blockFaceMask.HasFlagFast(BlockFaceMask.North) && isNorthVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.East) && isEastVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.South) && isSouthVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.West) && isWestVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.Up) && isUpVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;

				if (blockFaceMask.HasFlagFast(BlockFaceMask.Down) && isDownVisible) { indices.AddRange([ offset + 0u, offset + 1u, offset + 2u, offset + 2u, offset + 3u, offset + 0u, ]); }
				offset += VerticesPerFace;
			}

			return indices.ToArray();
		}

		[MustUseReturnValue]
		public static ChunkVertex[] GetChunkVertices() {
			ChunkVertex[] chunkVertices = new ChunkVertex[Chunk.ArraySize * FaceCount * VerticesPerFace];

			for (ushort cubeIndex = 0; cubeIndex < Chunk.ArraySize; cubeIndex++) {
				Chunk.FromIndex(cubeIndex, out byte x, out byte y, out byte z);

				VertexXyzUv[] cubeVertices = CubeBuilder.BuildCube(BlockFaceMask.All, 1, x, y, z);
				int cubeIndexOffset = cubeIndex * FaceCount * VerticesPerFace;

				for (int j = 0; j < cubeVertices.Length; j++) {
					VertexXyzUv vertex = cubeVertices[j];
					chunkVertices[cubeIndexOffset + j] = new(vertex.X, vertex.Y, vertex.Z, vertex.U, vertex.V);
				}
			}

			return chunkVertices;
		}
	}
}