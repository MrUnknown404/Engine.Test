using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Engine3.Test.Core.Graphics {
	public unsafe class ModelsBuffer {
		private static byte MatrixSize { get; } = (byte)sizeof(Matrix4x4);

		public required Matrix4x4[] Models { get; init; }

		public ulong Count { get; }
		public ulong Size { get; }

		[SetsRequiredMembers]
		public ModelsBuffer(ulong count) {
			Models = new Matrix4x4[count];
			Count = count;
			Size = MatrixSize * Count;
		}
	}
}