using IntelligenceHub.Host.Swagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using System.Text.Json;

namespace IntelligenceHub.Tests.Unit.Host
{
    public class NullableRouteParametersOperationFilterTests
    {
        [Fact]
        public void Apply_MarksOptionalParameter()
        {
            var operation = new OpenApiOperation
            {
                Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter { Name = "id", Required = true }
                }
            };
            var apiDescription = new ApiDescription();
            apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
            {
                Name = "id",
                Source = BindingSource.Path,
                RouteInfo = new ApiParameterRouteInfo { IsOptional = true }
            });
            var schemaGen = new SchemaGenerator(new SchemaGeneratorOptions(), new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
            var context = new OperationFilterContext(apiDescription, schemaGen, new SchemaRepository(), typeof(object).GetMethod("ToString")!);
            var filter = new NullableRouteParametersOperationFilter();
            filter.Apply(operation, context);
            Assert.False(operation.Parameters[0].Required);
        }
    }
}