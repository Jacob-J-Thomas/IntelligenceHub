﻿using Microsoft.AspNetCore.Mvc;
using IntelligenceHub.Business;
using IntelligenceHub.Common.Extensions;
using IntelligenceHub.API.DTOs.RAG;
using IntelligenceHub.Common.Config;
using IntelligenceHub.Client;
using Microsoft.AspNetCore.Authorization;
using IntelligenceHub.Common;
using IntelligenceHub.API.DTOs;

namespace IntelligenceHub.Controllers
{
    [Route("Rag")]
    [Authorize(Policy = "AdminPolicy")]
    public class RagController : ControllerBase
    {
        private readonly IRagLogic _ragLogic;

        public RagController(IRagLogic ragLogic) 
        {
            _ragLogic = ragLogic;
        }

        [HttpGet]
        [Route("Index/{name}")]
        [ProducesResponseType(typeof(IndexMetadata), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpGet]
        [Route("Index/GetAll")]
        [ProducesResponseType(typeof(IEnumerable<IndexMetadata>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpPost]
        [Route("Index")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpDelete]
        [Route("Index/Delete/{index}")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpGet]
        [Route("Index/{index}/Document/{count}/Page/{page}")]
        [ProducesResponseType(typeof(IEnumerable<IndexDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDocuments([FromRoute] string index, [FromRoute] int count, [FromRoute] int page)
        {
            try
            {
                var response = await _ragLogic.GetAllDocuments(index, count, page); // going to need to add pagination here
                if (response != null && response.Count() > 1) return Ok(response);
                else return NotFound();
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpGet]
        [Route("index/{index}/document/{document}")]
        [ProducesResponseType(typeof(IndexDocument), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpPost]
        [Route("index/{index}/Document")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }

        [HttpDelete]
        [Route("index/{index}/Document/{commaDelimitedDocNames}")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, GlobalVariables.DefaultExceptionMessage);
            }
        }
    }
}
