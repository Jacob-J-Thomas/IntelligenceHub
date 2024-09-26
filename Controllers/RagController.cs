using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.API.DTOs.ClientDTOs.RagDTOs;
using IntelligenceHub.API.DTOs.DataAccessDTOs;
using IntelligenceHub.Business;
using IntelligenceHub.Host.Config;
using IntelligenceHub.Common.Extensions;

namespace IntelligenceHub.Controllers
{
    [Route("Rag")]
    public class RagController : ControllerBase
    {
        private readonly RagLogic _ragLogic;
        public RagController(Settings settings) 
        {
            _ragLogic = new(
                settings.DbConnectionString,
                settings.RagDbConnectionString,
                settings.AIEndpoint, 
                settings.AIKey, 
                settings.DefaultEmbeddingModel,
                settings.DefaultAGIModel);
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
        public async Task<IActionResult> CreateIndex([FromBody] RagIndexMetaDataDTO indexDefinition)
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
        public async Task<IActionResult> ConfigureIndex([FromRoute] string index, [FromBody] RagIndexMetaDataDTO indexDefinition)
        {
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
            throw new NotImplementedException("Direct queries have not been implemented for AI Search indexes for this API");
        }

        [HttpGet]
        [Route("index/{index}/document/GetAll")]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index)
        {
            try
            {
                var response = await _ragLogic.GetAllDocuments(index); // going to need to add pagination here
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

        [HttpGet]
        [Route("index/{index}/document/{document}")]
        public async Task<IActionResult> GetDocument([FromRoute] string index, [FromRoute] string document)
        {
            try
            {
                var response = await _ragLogic.GetDocument(index, document); // going to need to add pagination here
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
        public async Task<IActionResult> UpsertDocuments([FromRoute] string index, [FromBody] RagUpsertRequest documents)
        {
            try
            {
                var chunkedDocuments = await _ragLogic.ChunkDocuments(index, documents);
                var response = await _ragLogic.UpsertDocuments(index, chunkedDocuments);
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
                if (response == 0) return NotFound($"The index {index} does not exist, or does not contain any of the documents in the list");
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
