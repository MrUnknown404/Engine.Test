using System.Numerics;

namespace Engine3.Test.Core.Graphics {
	public readonly record struct CameraUniformBuffer {
		public Matrix4x4 Projection { get; init; } = Matrix4x4.Identity;
		public Matrix4x4 View { get; init; } = Matrix4x4.Identity;

		public CameraUniformBuffer(Matrix4x4 projection, Matrix4x4 view) {
			Projection = projection;
			View = view;
		}
	}
}