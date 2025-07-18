using IntelligenceHub.API.DTOs;
using IntelligenceHub.Business.Factories;
using IntelligenceHub.Business.Implementations;
using IntelligenceHub.Client.Interfaces;
using IntelligenceHub.DAL.Interfaces;
using Moq;
using System.Reflection;
using Xunit;
using static IntelligenceHub.Common.GlobalVariables;

namespace IntelligenceHub.Tests.Unit.Business
{
    public class CompletionLogicAdditionalTests
    {
        private CompletionLogic CreateLogic(Mock<IAGIClient> agiClientMock, out Mock<IAGIClientFactory> factoryMock)
        {
            factoryMock = new Mock<IAGIClientFactory>();
            factoryMock.Setup(f => f.GetClient(It.IsAny<AGIServiceHost?>())).Returns(agiClientMock.Object);
            var ragFactory = new Mock<IRagClientFactory>();
            var toolClient = new Mock<IToolClient>();
            var toolRepo = new Mock<IToolRepository>();
            var profileRepo = new Mock<IProfileRepository>();
            var msgRepo = new Mock<IMessageHistoryRepository>();
            var indexRepo = new Mock<IIndexMetaRepository>();
            return new CompletionLogic(factoryMock.Object, ragFactory.Object, toolClient.Object, toolRepo.Object, profileRepo.Object, msgRepo.Object, indexRepo.Object);
        }

        [Fact]
        public async Task GenerateImage_AppendsImageToLastMessage()
        {
            var agiMock = new Mock<IAGIClient>();
            agiMock.Setup(c => c.GenerateImage("prompt"))!.ReturnsAsync("imgdata");
            var logic = CreateLogic(agiMock, out _);
            var method = typeof(CompletionLogic).GetMethod("GenerateImage", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var messages = new List<Message> { new Message{ Role = Role.User, Content="hi" } };
            var task = (Task<List<Message>>)method.Invoke(logic, new object?[]{"{\"prompt\":\"prompt\"}", AGIServiceHost.Azure, messages})!;
            var result = await task;
            Assert.Equal("imgdata", result.Last().Base64Image);
        }

        [Fact]
        public void ShallowCloneAndModifyLast_ModifiesLastMessage()
        {
            var logic = CreateLogic(new Mock<IAGIClient>(), out _);
            var method = typeof(CompletionLogic).GetMethod("ShallowCloneAndModifyLast", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var original = new List<Message>{ new Message{Content="a"}, new Message{Content="b"} };
            var result = (List<Message>)method.Invoke(logic, new object?[]{original, "pre:"})!;
            Assert.Equal("pre:b", result.Last().Content);
            Assert.Equal("a", result.First().Content);
        }
    }
}
