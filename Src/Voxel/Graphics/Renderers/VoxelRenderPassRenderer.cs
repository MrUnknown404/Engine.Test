using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics.DataStructs;
using Engine3.Client.Graphics.ImGui;
using Engine3.Client.Graphics.ImGui.Makers;
using Engine3.Client.Graphics.ImGui.Providers;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Graphics.Vulkan.Renderers;
using Engine3.Test.Test.Graphics.Vulkan;
using Engine3.Test.Voxel.Graphics.ImGui;
using ImGuiNET;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Voxel.Graphics.Renderers;

public unsafe class VoxelRenderPassRenderer : VulkanRenderPassRenderer {
	internal WorldRenderPass WorldRenderPass { get; }
	internal ChunkOutlineRenderPass ChunkOutlineRenderPass { get; }

	private readonly DescriptorBuffers cameraUniformBuffer;

	private readonly VoxelTest game;
	private readonly Camera camera;

	public VoxelRenderPassRenderer(VoxelTest game, VulkanBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly assembly) : base(graphicsBackend, window, true) {
		this.game = game;

		cameraUniformBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
			VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

		CreateImGui(out ImGuiBackend backend, out ImGuiRenderer renderer);
		ImGuiBackend = backend;
		ImGuiRenderer = renderer;
		UseImGui = true;

		ImGuiBackend.ShowDebugUI = true;
		ImGuiBackend.DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, };

		this.camera = camera;

		WorldRenderPass = new(game, this, assembly, cameraUniformBuffer);
		ChunkOutlineRenderPass = new(game, this, assembly, cameraUniformBuffer);

		AddRenderPass(WorldRenderPass);
		AddRenderPass(new CubeRenderPass(this, assembly, cameraUniformBuffer));

		AddRenderPass(ChunkOutlineRenderPass);
		AddRenderPass(new XyzGizmoRenderPass(this, assembly, camera));
	}

	private void AddExtraDebugUI(float indentAmount) {
		ImGuiH.IndentedCollapsingHeader("Camera", indentAmount, DrawCamera);

		if (game.World != null) { ImGuiH.IndentedCollapsingHeader("World", indentAmount, DrawWorld, ImGuiTreeNodeFlags.DefaultOpen); }

		ImGuiNet.Text("test");

		return;

		void DrawCamera() => CameraImGuiMaker.ShowImGui(camera); // this should be faster than a lambda?
		void DrawWorld() => WorldImGuiMaker.ShowImGui(game.World);
	}

	protected override void CopyBuffers(float delta) {
		cameraUniformBuffer.Copy(new ProjectionView(camera.Projection, camera.View), FrameIndex); // TODO lerp camera position & rotation

		base.CopyBuffers(delta);
	}
}