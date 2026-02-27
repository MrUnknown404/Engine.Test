using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.ImGui.Makers;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Test.Graphics.Vulkan;
using ImGuiNET;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics {
	public unsafe class VoxelRenderPassRenderer : VulkanRenderPassRenderer {
		private readonly WorldRenderPass worldRenderPass;

		private readonly DescriptorBuffers cameraUniformBuffer;

		protected override DepthImage DepthImage { get; }

		private readonly Camera camera;

		public VoxelRenderPassRenderer(GameClient game, VulkanGraphicsBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly assembly) : base(graphicsBackend, window) {
			cameraUniformBuffer = window.LogicalGpu.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
				VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

			ImGuiBackend = new(window, MaxFramesInFlight, new DemoWindowImGui()) { ShowDebugUI = true, DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, }, };

			DepthImage = LogicalGpu.CreateDepthImage(TransferCommandPool, SwapChain.Extent);

			this.camera = camera;

			worldRenderPass = new(PhysicalGpu, LogicalGpu, SwapChain, TransferCommandPool, assembly, MaxFramesInFlight, cameraUniformBuffer);

			AddRenderPass(worldRenderPass);
			AddRenderPass(new CubeRenderPass(window.SelectedGpu, LogicalGpu, SwapChain, TransferCommandPool, assembly, MaxFramesInFlight, cameraUniformBuffer));
			AddRenderPass(new XyzGizmoRenderPass(LogicalGpu, SwapChain, TransferCommandPool, assembly, MaxFramesInFlight, camera));
		}

		private void AddExtraDebugUI(float indentAmount) {
			ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawFunc);

			ImGui.Text("test");

			return;

			void DrawFunc() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
		}

		protected override void CopyBuffers(float delta) {
			cameraUniformBuffer.Copy(new ProjectionView(camera.Projection, camera.View), FrameIndex); // TODO lerp camera position & rotation

			base.CopyBuffers(delta);
		}

		public void MarkChunkDirty() => worldRenderPass.MarkChunkDirty();
		public void SetWorld(World.World world) => worldRenderPass.World = world;
	}
}