using Engine3.Client.Graphics.Vertex;
using Engine3.Exceptions;
using Engine3.Test.Voxel.Blocks;
using Engine3.Test.Voxel.Graphics.Vertex;
using Engine3.Test.Voxel.World;
using JetBrains.Annotations;

namespace Engine3.Test.Voxel.Graphics {
	public static class ChunkMeshBuilder {
		private const byte FaceCount = 6;
		private const byte VerticesPerFace = 4;

		[MustUseReturnValue]
		public static uint[] CreateChunkIndices(World.World world, ChunkPos position, bool showBorder) { // TODO optimize. eventually use a compute shader?
			const byte ChunkSize = Chunk.Size - 1;

			List<uint> indices = new();
			if (!world.TryGetChunk(position, out Chunk? chunk)) { throw new Engine3Exception("Failed to get chunk"); }

			world.TryGetChunk(position.Offset(0, 0, -1), out Chunk? northChunk);
			world.TryGetChunk(position.Offset(1, 0, 0), out Chunk? eastChunk);
			world.TryGetChunk(position.Offset(0, 0, 1), out Chunk? southChunk);
			world.TryGetChunk(position.Offset(-1, 0, 0), out Chunk? westChunk);
			world.TryGetChunk(position.Offset(0, 1, 0), out Chunk? upChunk);
			world.TryGetChunk(position.Offset(0, -1, 0), out Chunk? downChunk);

			uint offset = 0;
			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Block block = chunk[i];

				BlockFaceMask blockFaceMask = block.Properties.SolidFaceMask;
				if (blockFaceMask == BlockFaceMask.None) {
					offset += VerticesPerFace * FaceCount;
					continue;
				}

				Chunk.FromIndex(i, out byte x, out byte y, out byte z);

				bool isNorthVisible = z == 0 ?
						northChunk != null ? !northChunk[x, y, ChunkSize].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South) : showBorder :
						!chunk[x, y, (byte)(z - 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South);

				bool isEastVisible = x == ChunkSize ?
						eastChunk != null ? !eastChunk[0, y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West) : showBorder :
						!chunk[(byte)(x + 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West);

				bool isSouthVisible = z == ChunkSize ?
						southChunk != null ? !southChunk[x, y, 0].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North) : showBorder :
						!chunk[x, y, (byte)(z + 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North);

				bool isWestVisible = x == 0 ?
						westChunk != null ? !westChunk[ChunkSize, y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East) : showBorder :
						!chunk[(byte)(x - 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East);

				bool isUpVisible = y == ChunkSize ?
						upChunk != null ? !upChunk[x, 0, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down) : showBorder :
						!chunk[x, (byte)(y + 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down);

				bool isDownVisible = y == 0 ?
						downChunk != null ? !downChunk[x, ChunkSize, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up) : showBorder :
						!chunk[x, (byte)(y - 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up); //

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
		public static uint[] CreateChunkIndices(Chunk chunk, bool showBorder) { // TODO optimize. eventually use a compute shader?
			const byte ChunkSize = Chunk.Size - 1;

			List<uint> indices = new();

			uint offset = 0;
			for (ushort i = 0; i < Chunk.ArraySize; i++) {
				Block block = chunk[i];

				BlockFaceMask blockFaceMask = block.Properties.SolidFaceMask;
				if (blockFaceMask == BlockFaceMask.None) {
					offset += VerticesPerFace * FaceCount;
					continue;
				}

				Chunk.FromIndex(i, out byte x, out byte y, out byte z);

				bool isNorthVisible = z == 0 ? showBorder : !chunk[x, y, (byte)(z - 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.South);
				bool isEastVisible = x == ChunkSize ? showBorder : !chunk[(byte)(x + 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.West);
				bool isSouthVisible = z == ChunkSize ? showBorder : !chunk[x, y, (byte)(z + 1)].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.North);
				bool isWestVisible = x == 0 ? showBorder : !chunk[(byte)(x - 1), y, z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.East);
				bool isUpVisible = y == ChunkSize ? showBorder : !chunk[x, (byte)(y + 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Down);
				bool isDownVisible = y == 0 ? showBorder : !chunk[x, (byte)(y - 1), z].Properties.SolidFaceMask.HasFlagFast(BlockFaceMask.Up);

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