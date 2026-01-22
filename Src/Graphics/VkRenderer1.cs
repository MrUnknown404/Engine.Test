using System.Numerics;
using System.Reflection;
using Engine3.Graphics;
using Engine3.Graphics.Vulkan;
using Engine3.Test.Graphics.Test;
using OpenTK.Graphics.Vulkan;
using USharpLibs.Common.Math;

namespace Engine3.Test.Graphics {
	public unsafe class VkRenderer1 : VkRenderer {
		private GraphicsPipeline? graphicsPipeline;

		private VkBuffer? vertexBuffer;
		private VkBuffer? indexBuffer;
		private VkDeviceMemory? vertexBufferMemory;
		private VkDeviceMemory? indexBufferMemory;

		private readonly VkBuffer[] uniformBuffers;
		private readonly VkDeviceMemory[] uniformBuffersMemory;
		private readonly void*[] uniformBuffersMapped;

		private readonly Camera camera;

		private readonly TestVertex[] vertices = [ new(-0.5f, -0.5f, 0, 1, 0, 0), new(0.5f, -0.5f, 0, 0, 1, 0), new(0.5f, 0.5f, 0, 0, 0, 1), new(-0.5f, 0.5f, 0, 1, 1, 1), ];
		private readonly uint[] indices = [ 0, 1, 2, 2, 3, 0, ];
		private readonly TestUniformBufferObject testUniformBufferObject = new();
		private readonly Assembly shaderAssembly;

		private VkDescriptorSet CurrentDescriptorSet => graphicsPipeline?.DescriptorSets?[CurrentFrame] ?? throw new NullReferenceException("No graphics pipeline");

		public VkRenderer1(VkWindow window, byte maxFramesInFlight, Assembly shaderAssembly) : base(window, maxFramesInFlight) {
			this.shaderAssembly = shaderAssembly;

			uniformBuffers = new VkBuffer[MaxFramesInFlight];
			uniformBuffersMemory = new VkDeviceMemory[MaxFramesInFlight];
			uniformBuffersMapped = new void*[MaxFramesInFlight];

			camera = new PerspectiveCamera((float)SwapChain.Extent.width / SwapChain.Extent.height, 0.5f, 10f) { Position = new(0, 0, 3), YawDegrees = 270, };
			// camera = new OrthographicCamera(10, 10, 0.5f, 10f) { Position = new(0, 1, 3), YawDegrees = 270, };
		}

		public override void Setup() {
			ShaderModule vertexShaderModule = new(LogicalDevice, "GLSL.Test", ShaderLanguage.Glsl, ShaderType.Vertex, shaderAssembly);
			ShaderModule fragmentShaderModule = new(LogicalDevice, "GLSL.Test", ShaderLanguage.Glsl, ShaderType.Fragment, shaderAssembly);
			ShaderStageInfo[] shaderCreateInfos = [
					new(LogicalDevice, vertexShaderModule.VkShaderModule, VkShaderStageFlagBits.ShaderStageVertexBit), new(LogicalDevice, fragmentShaderModule.VkShaderModule, VkShaderStageFlagBits.ShaderStageFragmentBit),
			];

			uint uniformBufferSize = TestUniformBufferObject.Size;
			CreateUniformBuffers();

			GraphicsPipeline.Builder builder = new(LogicalDevice, SwapChain, shaderCreateInfos, TestVertex.GetAttributeDescriptions(), TestVertex.GetBindingDescriptions()) { CullMode = VkCullModeFlagBits.CullModeNone, };
			builder.AddDescriptorSets(VkShaderStageFlagBits.ShaderStageVertexBit, 0, MaxFramesInFlight, uniformBuffers, uniformBufferSize);
			graphicsPipeline = builder.MakePipeline();

			vertexShaderModule.Cleanup();
			fragmentShaderModule.Cleanup();

			CreateBufferUsingStagingBuffer(PhysicalDevice, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, vertices, VkBufferUsageFlagBits.BufferUsageVertexBufferBit, out vertexBuffer, out vertexBufferMemory);
			CreateBufferUsingStagingBuffer(PhysicalDevice, LogicalDevice, TransferCommandPool, LogicalGpu.TransferQueue, indices, VkBufferUsageFlagBits.BufferUsageIndexBufferBit, out indexBuffer, out indexBufferMemory);

			return;

			void CreateUniformBuffers() {
				fixed (void** uniformBufferMapped = uniformBuffersMapped) {
					for (int i = 0; i < MaxFramesInFlight; i++) {
						VkH.CreateBufferAndMemory(PhysicalDevice, LogicalDevice, VkBufferUsageFlagBits.BufferUsageUniformBufferBit,
							VkMemoryPropertyFlagBits.MemoryPropertyHostVisibleBit | VkMemoryPropertyFlagBits.MemoryPropertyHostCoherentBit, uniformBufferSize, out uniformBuffers[i], out uniformBuffersMemory[i]);

						Vk.MapMemory(LogicalDevice, uniformBuffersMemory[i], 0, uniformBufferSize, 0, &uniformBufferMapped[i]);
					}
				}
			}

			static void CreateBufferUsingStagingBuffer<T>(VkPhysicalDevice physicalDevice, VkDevice logicalDevice, VkCommandPool transferPool, VkQueue transferQueue, T[] bufferData, VkBufferUsageFlagBits bufferUsage,
				out VkBuffer? buffer, out VkDeviceMemory? bufferMemory) where T : unmanaged {
				VkH.CreateBufferUsingStagingBuffer(physicalDevice, logicalDevice, transferPool, transferQueue, bufferData, bufferUsage, out VkBuffer vertexBuffer, out VkDeviceMemory vertexBufferMemory);

				buffer = vertexBuffer;
				bufferMemory = vertexBufferMemory;
			}
		}

		protected override void UpdateUniformBuffer(float delta) {
			// camera.YawDegrees += 0.05f;

			testUniformBufferObject.Projection = camera.CreateProjectionMatrix();
			testUniformBufferObject.View = camera.CreateViewMatrix();
			testUniformBufferObject.Model = Matrix4x4.CreateRotationX(FrameCount / 1000f * MathH.ToRadians(90f)); // TODO currently affected by frame rate

			byte[] data = testUniformBufferObject.CollectBytes();
			fixed (void* dataPtr = data) { Buffer.MemoryCopy(dataPtr, uniformBuffersMapped[CurrentFrame], (ulong)data.Length, (ulong)data.Length); }
		}

		protected override void DrawFrame(VkCommandBuffer graphicsCommandBuffer, float delta) {
			if (this.graphicsPipeline is not { } graphicsPipeline) { return; }
			if (this.vertexBuffer is not { } vertexBuffer) { return; }
			if (this.indexBuffer is not { } indexBuffer) { return; }

			graphicsPipeline.CmdBind(graphicsCommandBuffer);

			VkH.CmdSetViewport(graphicsCommandBuffer, 0, 0, SwapChain.Extent.width, SwapChain.Extent.height, 0, 1);
			VkH.CmdSetScissor(graphicsCommandBuffer, SwapChain.Extent, new(0, 0));

			VkH.CmdBindVertexBuffer(graphicsCommandBuffer, vertexBuffer, 0);
			Vk.CmdBindIndexBuffer(graphicsCommandBuffer, indexBuffer, 0, VkIndexType.IndexTypeUint32);

			VkDescriptorSet descriptorSet = CurrentDescriptorSet;
			VkBindDescriptorSetsInfo bindDescriptorSetsInfo = new() { layout = graphicsPipeline.Layout, descriptorSetCount = 1, pDescriptorSets = &descriptorSet, stageFlags = VkShaderStageFlagBits.ShaderStageVertexBit, };

			Vk.CmdBindDescriptorSets2(graphicsCommandBuffer, &bindDescriptorSetsInfo);

			Vk.CmdDrawIndexed(graphicsCommandBuffer, (uint)indices.Length, 1, 0, 0, 0);
		}

		protected override void Cleanup() {
			Vk.DeviceWaitIdle(LogicalDevice);

			if (this.vertexBuffer is { } vertexBuffer) { Vk.DestroyBuffer(LogicalDevice, vertexBuffer, null); }
			if (this.vertexBufferMemory is { } vertexBufferMemory) { Vk.FreeMemory(LogicalDevice, vertexBufferMemory, null); }

			if (this.indexBuffer is { } indexBuffer) { Vk.DestroyBuffer(LogicalDevice, indexBuffer, null); }
			if (this.indexBufferMemory is { } indexBufferMemory) { Vk.FreeMemory(LogicalDevice, indexBufferMemory, null); }

			foreach (VkBuffer uniformBuffer in uniformBuffers) { Vk.DestroyBuffer(LogicalDevice, uniformBuffer, null); }

			foreach (VkDeviceMemory uniformBufferMemory in uniformBuffersMemory) {
				//Vk.UnmapMemory(LogicalDevice, uniformBufferMemory); // i don't think i need to call this?
				Vk.FreeMemory(LogicalDevice, uniformBufferMemory, null);
			}

			graphicsPipeline?.Cleanup();

			base.Cleanup();
		}
	}
}