using System.Numerics;
using System.Reflection;
using Engine3.Exceptions;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Graphics.Vulkan.Objects;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine3.Test.Graphics.Vulkan {
	public unsafe class VkRenderer1 : VkRenderer {
		private const string TestShaderName = "Test";

		private GraphicsPipeline? graphicsPipeline;

		private VkBufferObject? vertexBuffer;
		private VkBufferObject? indexBuffer;

		private readonly VkBufferObject[] uniformBuffers;
		private readonly void*[] uniformBuffersMapped;

		private readonly Camera camera;

		private readonly TestVertex[] vertices = [ new(-0.5f, -0.5f, 0, 1, 0, 0), new(0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, 0.5f, 0, 0, 0, 1), new(-0.5f, 0.5f, 0, 1, 1, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, 2, 3, 0, ];
		private readonly TestUniformBufferObject testUniformBufferObject = new();
		private readonly Assembly shaderAssembly;

		public VkRenderer1(GameClient gameClient, VkWindow window, Assembly shaderAssembly) : base(gameClient, window) {
			this.shaderAssembly = shaderAssembly;

			uniformBuffers = new VkBufferObject[MaxFramesInFlight];
			uniformBuffersMapped = new void*[MaxFramesInFlight];

			// camera = new OrthographicCamera(10, 10, 0.5f, 10f) { Position = new(0, 1, 3), YawDegrees = 270, };
			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.5f, 10f) { Position = new(0, 0, 3), YawDegrees = 270, };
		}

		public override void Setup() {
			VkShaderObject vertexShader = new("Test Vertex Shader", LogicalDevice, TestShaderName, ShaderLanguage.Glsl, ShaderType.Vertex, shaderAssembly);
			VkShaderObject fragmentShader = new("", LogicalDevice, TestShaderName, ShaderLanguage.Glsl, ShaderType.Fragment, shaderAssembly);

			uint uniformBufferSize = TestUniformBufferObject.Size;
			CreateUniformBuffers();

			GraphicsPipeline.Builder builder =
					new("Test Graphics Pipeline", LogicalDevice, SwapChain, [ vertexShader, fragmentShader, ], TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions()) {
							CullMode = VkCullModeFlagBits.CullModeNone,
					};

			builder.AddDescriptorSets(VkShaderStageFlagBits.ShaderStageVertexBit, 0, MaxFramesInFlight, uniformBuffers.Select(static buffer => buffer.Buffer).ToArray(), uniformBufferSize);
			graphicsPipeline = builder.MakePipeline();

			vertexShader.Destroy();
			fragmentShader.Destroy();

			vertexBuffer = new("Test Vertex Buffer", (ulong)(sizeof(TestVertex) * vertices.Length), PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageVertexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			indexBuffer = new("Test Index Buffer", (ulong)(sizeof(uint) * indices.Length), PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageTransferDstBit | VkBufferUsageFlagBits.BufferUsageIndexBufferBit,
				VkMemoryPropertyFlagBits.MemoryPropertyDeviceLocalBit);

			vertexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, vertices);
			indexBuffer.CopyUsingStaging(TransferCommandPool, LogicalGpu.TransferQueue, indices);

			return;

			void CreateUniformBuffers() {
				fixed (void** uniformBufferMapped = uniformBuffersMapped) {
					for (int i = 0; i < MaxFramesInFlight; i++) {
						VkBufferObject uniformBuffer = new($"Test Uniform Buffer[{i}]", uniformBufferSize, PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
							VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit);

						uniformBuffers[i] = uniformBuffer;
						uniformBufferMapped[i] = uniformBuffer.MapMemory(uniformBufferSize);
					}
				}
			}
		}

		protected override void UpdateUniformBuffer(float delta) {
			// camera.YawDegrees += 0.05f;

			testUniformBufferObject.Projection = camera.CreateProjectionMatrix();
			testUniformBufferObject.View = camera.CreateViewMatrix();
			testUniformBufferObject.Model = Matrix4x4.CreateRotationY(FrameCount / 1000f * MathH.ToRadians(90f)); // TODO currently affected by frame rate

			byte[] data = testUniformBufferObject.CollectBytes();
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
		}

		protected override void RecordCommandBuffer(GraphicsCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.graphicsPipeline is not { } graphicsPipeline) { return; }
			if (this.vertexBuffer is not { } vertexBuffer) { return; }
			if (this.indexBuffer is not { } indexBuffer) { return; }

			graphicsCommandBuffer.CmdBindGraphicsPipeline(graphicsPipeline.Pipeline);

			graphicsCommandBuffer.CmdSetViewport(0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			graphicsCommandBuffer.CmdSetScissor(SwapChain.Extent, new(0, 0));

			graphicsCommandBuffer.CmdBindVertexBuffer(vertexBuffer.Buffer, 0);
			graphicsCommandBuffer.CmdBindIndexBuffer(indexBuffer.Buffer, indexBuffer.BufferSize);

			graphicsCommandBuffer.CmdBindDescriptorSets(graphicsPipeline.Layout, graphicsPipeline.DescriptorSets?[CurrentFrame] ?? throw new VulkanException("Uniform Buffer needed fpr this"),
				VkShaderStageFlagBits.ShaderStageVertexBit);

			graphicsCommandBuffer.CmdDrawIndexed((uint)indices.Length);
		}

		protected override void Cleanup() {
			vertexBuffer?.Destroy();
			indexBuffer?.Destroy();
			foreach (VkBufferObject uniformBuffer in uniformBuffers) { uniformBuffer.Destroy(); }

			graphicsPipeline?.Destroy();
		}
	}
}