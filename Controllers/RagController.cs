using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Client;
using IntelligenceHub.Common.Exceptions;

namespace IntelligenceHub.Controllers
{
    [Route("Rag")]
    public class RagController : ControllerBase
    {
        private readonly RagLogic _ragLogic;

        public RagController(IAGIClient agiClient, IAISearchServiceClient searchClient, Settings settings) 
        {
            _ragLogic = new RagLogic(agiClient, searchClient, settings.DbConnectionString);
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, ex.Message);
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
            }
        }

        [HttpPost]
        [Route("Index")]
        public async Task<IActionResult> CreateIndex([FromBody] IndexMetadata indexDefinition)
        {
            try
            {
                var response = await _ragLogic.CreateIndex(indexDefinition);
                if (response) return Ok();
                else return BadRequest();
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
            }
        }

        [HttpPost]
        [Route("Index/Query/{index}")]
        public async Task<IActionResult> QueryIndex([FromRoute] string index, [FromBody] Object request)
        {
            throw new NotImplementedException("Currently this API recommends making direct queries to the AI Search API");
        }

        [HttpGet]
        [Route("Index/{index}/Document/{count}/Page/{page}")]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index, [FromRoute] int count, [FromRoute] int page)
        {
            try
            {
                var response = await _ragLogic.GetAllDocuments(index, count, page); // going to need to add pagination here
                if (response != null && response.Count() > 1) return Ok(response);
                else return NotFound();
            }
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
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
            catch (IntelligenceHubException hubEx)
            {
                if (hubEx.StatusCode == 404) return NotFound(hubEx.Message);
                else if (hubEx.StatusCode > 399 && hubEx.StatusCode < 500) return BadRequest(hubEx.Message);
                else throw;
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                throw new IntelligenceHubException(500, "Internal Server Error: Please reattempt. If this issue persists please contact the system administrator.");
            }
        }
    }
}
