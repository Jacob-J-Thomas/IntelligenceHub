using Microsoft.AspNetCore.Mvc;
using OpenAICustomFunctionCallingAPI.API.DTOs.ClientDTOs.RagDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.ControllerDTOs;
using OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs;
using OpenAICustomFunctionCallingAPI.Business;
using OpenAICustomFunctionCallingAPI.Host.Config;

namespace OpenAICustomFunctionCallingAPI.Controllers
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
        [Route("Index/Get/{name}")]
        public async Task<IActionResult> Get([FromRoute] string name)
        {
            var response = await _ragLogic.GetRagIndex(name);
            if (response == null) return NotFound();
            return Ok(response);
        }

        [HttpGet]
        [Route("Index/GetAll")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _ragLogic.GetAllIndexesAsync();
                if (response != null && response.Count() > 0) return Ok(response);
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Index/Create")]
        public async Task<IActionResult> CreateIndex([FromBody] RagIndexMetaDataDTO indexDefinition)
        {
            try
            {
                var response = await _ragLogic.CreateIndex(indexDefinition);
                if (response) return Ok();
                return BadRequest();
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
                return NotFound();
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
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Index/Query/{index}")]
        public async Task<IActionResult> QueryIndex([FromRoute] string index, [FromBody] DirectQueryRequest request)
        {
            try
            {
                var response = await _ragLogic.QueryIndex(index, request);
                if (response != null) return Ok(response);
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("Document/GetAll/{index}")]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index)
        {
            try
            {
                var response = await _ragLogic.GetAllDocuments(index); // going to need to add pagination here
                if (response != null) return Ok(response);
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet]
        [Route("Document/Get/{index}/{document}")]
        public async Task<IActionResult> GetDocument([FromRoute] string index, [FromRoute] string document)
        {
            try
            {
                var response = await _ragLogic.GetDocument(index, document); // going to need to add pagination here
                if (response != null) return Ok(response);
                return NotFound();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("Document/Upsert/{index}")]
        public async Task<IActionResult> UpsertDocuments([FromRoute] string index, [FromBody] RagUpsertRequest documents)
        {
            try
            {
                var chunkedDocuments = await _ragLogic.ChunkDocuments(index, documents);
                var response = await _ragLogic.UpsertDocuments(index, chunkedDocuments);
                if (response) return Ok(response);
                return BadRequest();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpDelete]
        [Route("{index}/Document/Delete")]
        public async Task<IActionResult> DeleteDocuments([FromRoute] string index, [FromBody] string[] documents)
        {
            try
            {
                var response = await _ragLogic.DeleteDocuments(index, documents);
                if (response == 0) return NotFound();
                return Ok(response);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
