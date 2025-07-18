using IntelligenceHub.API.DTOs;
using static IntelligenceHub.Common.GlobalVariables;
using Xunit;

namespace IntelligenceHub.Tests.Unit.API
{
    public class APIResponseWrapperTests
    {
        [Fact]
        public void Success_WrapsData()
        {
            var wrapper = APIResponseWrapper<string>.Success("ok");
            Assert.True(wrapper.IsSuccess);
            Assert.Equal("ok", wrapper.Data);
            Assert.Equal(APIResponseStatusCodes.Ok, wrapper.StatusCode);
            Assert.Null(wrapper.ErrorMessage);
        }

        [Fact]
        public void Failure_WithMessage()
        {
            var wrapper = APIResponseWrapper<string>.Failure("bad", APIResponseStatusCodes.BadRequest);
            Assert.False(wrapper.IsSuccess);
            Assert.Equal("bad", wrapper.ErrorMessage);
            Assert.Equal(APIResponseStatusCodes.BadRequest, wrapper.StatusCode);
        }

        [Fact]
        public void Failure_WithDataAndMessage()
        {
            var wrapper = APIResponseWrapper<int>.Failure(5, "error", APIResponseStatusCodes.InternalError);
            Assert.False(wrapper.IsSuccess);
            Assert.Equal(5, wrapper.Data);
            Assert.Equal("error", wrapper.ErrorMessage);
            Assert.Equal(APIResponseStatusCodes.InternalError, wrapper.StatusCode);
        }
    }
}