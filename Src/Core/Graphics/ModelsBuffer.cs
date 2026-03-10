using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Engine3.Test.Core.Graphics {
	public unsafe class ModelsBuffer {
		private static byte MatrixSize { get; } = (byte)sizeof(Matrix4x4);

		public required Matrix4x4[] Models { get; init; }

		public uint Count { get; }
		public ulong Size { get; }

		[SetsRequiredMembers]
		public ModelsBuffer(uint count) {
			Models = new Matrix4x4[count];
			Count = count;
			Size = MatrixSize * Count;
		}
	}
}