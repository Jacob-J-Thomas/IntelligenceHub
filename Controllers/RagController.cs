using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Config;

namespace IntelligenceHub.Controllers
{
    [Route("Rag")]
    public class RagController : ControllerBase
    {
        private readonly RagLogic _ragLogic;
        public RagController(Settings settings, AIClientSettings aiClientSettings, SearchServiceClientSettings searchClientSettings) 
        {
            _ragLogic = new RagLogic(
                settings.DbConnectionString,
                settings.DbConnectionString,
                aiClientSettings.Endpoint,
                aiClientSettings.Key,
                searchClientSettings.Endpoint,
                searchClientSettings.Key);
        }

        [HttpGet]
        [Route("Index/{name}")]
        public async Task<IActionResult> Get([FromRoute] string name)
        {
            try
            {
                var response = await _ragLogic.GetRagIndex(name);
                if (response is not null) return NotFound();
                else return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("Index/GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _ragLogic.GetAllIndexesAsync();
                if (response is not null && response.Count() > 0) return Ok(response);
                else return NotFound();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Index/")]
        public async Task<IActionResult> CreateIndex([FromBody] IndexMetadata indexDefinition)
        {
            try
            {
                var response = await _ragLogic.CreateIndex(indexDefinition);
                if (response) return Ok();
                else return BadRequest();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Index/Configure/{index}")]
        public async Task<IActionResult> ConfigureIndex([FromRoute] string index, [FromBody] IndexMetadata indexDefinition)
        {
            throw new NotImplementedException("Currently index updating is not supported. Please delete and rebuild the index to modify its definition.");

            try
            {
                var response = await _ragLogic.ConfigureIndex(indexDefinition);
                if (response) return Ok();
                else return NotFound();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpDelete]
        [Route("Index/Delete/{index}")]
        public async Task<IActionResult> DeleteIndex([FromRoute] string index)
        {
            try
            {
                var response = await _ragLogic.DeleteIndex(index);
                if (response) return Ok();
                else return NotFound();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Index/Query/{index}")]
        public async Task<IActionResult> QueryIndex([FromRoute] string index, [FromBody] Object request)
        {
            throw new NotImplementedException("Currently this API recommends making direct queries to the AI Search API");
        }

        [HttpGet]
        [Route("index/{index}/document/GetAll")]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index)
        {
            try
            {
                var response = await _ragLogic.GetAllDocuments(index); // going to need to add pagination here
                if (response != null && response.Count() > 1) return Ok(response);
                else return NotFound();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("index/{index}/document/{document}")]
        public async Task<IActionResult> GetDocument([FromRoute] string index, [FromRoute] string document)
        {
            try
            {
                var response = await _ragLogic.GetDocument(index, document);
                if (response is not null) return Ok(response);
                else return NotFound();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("index/{index}/Document")]
        public async Task<IActionResult> UpsertDocuments([FromRoute] string index, [FromBody] RagUpsertRequest documentUpsertRequest)
        {
            try
            {
                var response = await _ragLogic.UpsertDocuments(index, documentUpsertRequest);
                if (response) return Ok(response);
                else return BadRequest();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpDelete]
        [Route("index/{index}/Document/{commaDelimitedDocNames}")]
        public async Task<IActionResult> DeleteDocuments([FromRoute] string index, [FromRoute] string commaDelimitedDocNames)
        {
            try
            {
                var documents = commaDelimitedDocNames.ToStringArray();
                var response = await _ragLogic.DeleteDocuments(index, documents);
                if (response < 1) return NotFound($"The index {index} does not exist, or does not contain any of the documents in the list");
                else return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
