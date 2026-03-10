using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.ImGui.Makers;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Test.Graphics.Vulkan;
using Engine3.Test.Voxel.Graphics.ImGui;
using ImGuiNET;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Renderers {
	public unsafe class VoxelRenderPassRenderer : VulkanRenderPassRenderer {
		internal WorldRenderPass WorldRenderPass { get; }
		internal ChunkOutlineRenderPass ChunkOutlineRenderPass { get; }

		private readonly DescriptorBuffers cameraUniformBuffer;

		private readonly Camera camera;

		public VoxelRenderPassRenderer(GameClient game, VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly assembly) : base(graphicsBackend, window, true) {
			cameraUniformBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			ImGuiBackend = new(window, MaxFramesInFlight, new DemoWindowImGui()) { ShowDebugUI = true, DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, }, };

			this.camera = camera;

			WorldRenderPass = new(this, assembly, cameraUniformBuffer);
			ChunkOutlineRenderPass = new(this, assembly, cameraUniformBuffer);

			AddRenderPass(WorldRenderPass);
			AddRenderPass(new CubeRenderPass(this, assembly, cameraUniformBuffer));

			AddRenderPass(ChunkOutlineRenderPass);
			AddRenderPass(new XyzGizmoRenderPass(this, assembly, camera));
		}

		private void AddExtraDebugUI(float indentAmount) {
			ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawCamera);

			if (WorldRenderPass.World != null) { ImGuiH.IndentedCollapsingHeader("World", indentAmount, DrawWorld, ImGuiTreeNodeFlags.DefaultOpen); }

			ImGuiNet.Text("test");

			return;

			void DrawCamera() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
			void DrawWorld() => WorldImGuiMaker.ShowImGui(WorldRenderPass.World);
		}

		protected override void CopyBuffers(float delta) {
			cameraUniformBuffer.Copy(new ProjectionView(camera.Projection, camera.View), FrameIndex); // TODO lerp camera position & rotation

			base.CopyBuffers(delta);
		}

		public void SetWorld(World.World world) => WorldRenderPass.World = world;
	}
}