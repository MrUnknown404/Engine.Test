using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Engine3.Client;
using Engine3.Client.Graphics;
using Engine3.Client.Graphics.Vulkan;
using Engine3.Client.Graphics.Vulkan.Objects;
using Engine3.Test.Core.Graphics;
using NLog;
using OpenTK.Graphics.Vulkan;

namespace Engine3.Test.LightCycle.Graphics {
	public unsafe class VulkanLightCycleRenderer : VulkanRenderer {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private const string ShaderName = "Cycle";

		private readonly Assembly assembly = typeof(VulkanLightCycleRenderer).Assembly;

		private readonly GameManager gameManager;
		private readonly Camera camera;

		private GraphicsPipeline? graphicsPipeline;

		private DepthImage? depthImage;

		private VulkanBuffer? cubeVertexBuffer;
		private VulkanBuffer? cubeIndexBuffer;
		private DescriptorBuffers? cubeUniformBuffers;
		private DescriptorSets? cubeDescriptorSet;

		private readonly VertexXyzRgb[] cubeVertices;
		private readonly uint[] cubeIndices = [
				6, 2, 3, 3, 7, 6, // X-
				4, 0, 1, 1, 5, 4, // X+
				0, 1, 2, 2, 3, 0, // Y-
				4, 5, 6, 6, 7, 4, // Y+
				7, 3, 0, 0, 4, 7, // Z-
				5, 1, 2, 2, 6, 5, // Z+ (textured atm)
		];

		private readonly TestUniformBufferObject cubeUniformBufferObject = new();

		protected override DepthImage? DepthImage => depthImage;

		public VulkanLightCycleRenderer(VulkanGraphicsBackend graphicsBackend, VulkanWindow window, GameManager gameManager) : base(graphicsBackend, window) {
			this.gameManager = gameManager;

			camera = Camera.CreatePerspective((float)SwapChain.Extent.width / SwapChain.Extent.height, 90, 0.01f, 10f);
			camera.Position = new(0, 5, 0);
			camera.YawDegrees = 0;
			camera.PitchDegrees = -90;

			cubeUniformBufferObject.Projection = camera.Projection;

			const float Size = 0.1f;
			const float H = Size / 2;
			const float R = 1, G = 1, B = 1;

			const float X0 = -H, X1 = +H;
			const float Y0 = -H, Y1 = +H;
			const float Z0 = -H, Z1 = +H;

			cubeVertices = [
					new(X1, Y0, Z0, R, G, B), // 0
					new(X1, Y0, Z1, R, G, B), // 1
					new(X0, Y0, Z1, R, G, B), // 2
					new(X0, Y0, Z0, R, G, B), // 3
					new(X1, Y1, Z0, R, G, B), // 4
					new(X1, Y1, Z1, R, G, B), // 5
					new(X0, Y1, Z1, R, G, B), // 6
					new(X0, Y1, Z0, R, G, B), // 7
			];
		}

		public override void Setup() {
			base.Setup();

			CreateGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout);

			CreateBuffers();

			CreateDescriptorSets(descriptorSetLayout.VkDescriptorSetLayout);
			UpdateDescriptorSets();

			depthImage = LogicalGpu.CreateDepthImage(TransferCommandPool, SwapChain.Extent);
		}

		private void CreateGraphicsPipeline(out DescriptorSetLayout descriptorSetLayout) {
			VulkanShader vertexShader = LogicalGpu.CreateShader($"{ShaderName} Vertex Shader", ShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, assembly);
			VulkanShader fragmentShader = LogicalGpu.CreateShader($"{ShaderName} Fragment Shader", ShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, assembly);

			descriptorSetLayout = LogicalGpu.CreateDescriptorSetLayout([ new(VkDescriptorType.DescriptorTypeUniformBuffer, VkShaderStageFlagBits.ShaderStageVertexBit, 0), ]);

			// ew
			graphicsPipeline = LogicalGpu.CreateGraphicsPipeline(
				new("Test Graphics Pipeline", SwapChain.ImageFormat, [ vertexShader, fragmentShader, ], VertexXyzRgb.GetAttributeDescriptions(), VertexXyzRgb.GetBindingDescriptions()) {
						DescriptorSetLayouts = [ descriptorSetLayout.VkDescriptorSetLayout, ],
						// FrontFace = VkFrontFace.FrontFaceCounterClockwise, // TODO oops. indices are backwards
						CullMode = VkCullModeFlagBits.CullModeNone,
				});

			Logger.Debug("Created graphics pipeline");

			LogicalGpu.EnqueueDestroy(vertexShader);
			LogicalGpu.EnqueueDestroy(fragmentShader);
		}

		private void CreateBuffers() {
			cubeVertexBuffer = LogicalGpu.CreateBuffer("Cube Vertex Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit, (ulong)(sizeof(VertexXyzRgb) * cubeVertices.Length));

			cubeIndexBuffer = LogicalGpu.CreateBuffer("Cube Index Buffer", VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit, VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit,
				(ulong)(sizeof(uint) * cubeIndices.Length));

			TransferCommandPool.CopyToBuffers([ TransferCommandPool.CopyDataToBufferInfo.Copy(cubeVertexBuffer, cubeVertices), TransferCommandPool.CopyDataToBufferInfo.Copy(cubeIndexBuffer, cubeIndices), ]);

			ulong bufferSize = TestUniformBufferObject.Size;
			cubeUniformBuffers = LogicalGpu.CreateDescriptorBuffers("Cube Uniform Buffers", bufferSize, MaxFramesInFlight, VkDescriptorType.DescriptorTypeUniformBuffer, VkBufferUsageFlagBits.BufferUsageUniformBufferBit);
			Logger.Debug("Created uniform buffers");
		}

		private void CreateDescriptorSets(VkDescriptorSetLayout descriptorSetLayout) {
			DescriptorPool descriptorPool = LogicalGpu.CreateDescriptorPool([ VkDescriptorType.DescriptorTypeUniformBuffer, ], 1, MaxFramesInFlight);
			cubeDescriptorSet = descriptorPool.AllocateDescriptorSet(descriptorSetLayout);
			Logger.Debug("Created descriptor sets");
		}

		private void UpdateDescriptorSets() {
			if (cubeUniformBuffers == null || cubeDescriptorSet == null) { throw new UnreachableException(); }

			cubeDescriptorSet.UpdateDescriptorSet(0, cubeUniformBuffers);

			Logger.Debug("Updated descriptor sets");
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer) {
			if (graphicsPipeline == null || cubeVertexBuffer == null || cubeIndexBuffer == null || cubeDescriptorSet == null) { throw new NullReferenceException(); }

			if (gameManager.Map is not { } map) {
				Logger.Warn("No map found. Nothing to render. Do something about that"); // TODO impl rendering when we have no map
				return;
			}

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline); // TODO integrate graphics pipeline binding into VulkanRenderer?

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(0, 0, SwapChain.Extent);

			// Cube
			graphicsCommandBuffer.CmdBindDescriptorSet(graphicsPipeline.Layout, cubeDescriptorSet.GetCurrent(FrameIndex), VkShaderStageFlagBits.ShaderStageVertexBit);
			graphicsCommandBuffer.CmdBindVertexBuffer(cubeVertexBuffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(cubeIndexBuffer, cubeIndexBuffer.BufferSize);
			graphicsCommandBuffer.CmdDrawIndexed((uint)cubeIndices.Length);
		}

		protected override void CopyBuffers(float delta) {
			if (cubeUniformBuffers == null) { throw new UnreachableException(); }

			if (gameManager.Map is not { } map) { return; }

			Cycle.Cycle cycle = map.Cycles.First();

			cubeUniformBufferObject.View = camera.View;
			cubeUniformBufferObject.Model = Matrix4x4.CreateRotationY(float.Lerp(LightCycleTest.PrevCubeRotation, LightCycleTest.CubeRotation, delta) * float.DegreesToRadians(90f)) *
											cycle.Transform.CreateMatrix(delta, cycle.PreviousTransform);

			cubeUniformBuffers.Copy(cubeUniformBufferObject.CollectBytes(), FrameIndex);
		}
	}
}