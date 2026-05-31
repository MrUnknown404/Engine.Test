using System.Reflection;
using Engine3.Client.Client;
using Engine3.Client.Client.Graphics.DataStructs;
using Engine3.Client.Client.ImGui;
using Engine3.Client.Client.Graphics.Vulkan;
using Engine3.Client.Client.Graphics.Vulkan.Objects;
using Engine3.Client.Client.Graphics.Vulkan.Renderers;
using Engine3.Core;
using ImGuiNET;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.Test.Graphics.Vulkan;

public unsafe class TestRenderPassRenderer : VulkanRenderPassRenderer {
	private readonly DescriptorBuffers cameraUniformBuffer;

	private readonly Camera camera;

	public TestRenderPassRenderer(EngineGame game, VulkanBackend graphicsBackend, VulkanWindow window, Camera camera, Assembly assembly) : base(graphicsBackend, window, true) {
		cameraUniformBuffer = GraphicsResourceProvider.CreateDescriptorBuffers("Camera Uniform Buffer", (ulong)sizeof(ProjectionView), MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer,
			VkBufferUsageFlagBits.BufferUsageUniformBufferBit);

		CreateImGui(out ImGuiBackend backend, out ImGuiRenderer renderer);
		ImGuiBackend = backend;
		ImGuiRenderer = renderer;
		UseImGui = true;

		ImGuiBackend.ShowDebugUI = true;
		ImGuiBackend.DebugUIImGui = new DebugUIImGui(game, window) { AddExtraDebugUI = AddExtraDebugUI, };

		this.camera = camera;

		AddRenderPass(new CubeRenderPass(this, assembly, cameraUniformBuffer));
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
}