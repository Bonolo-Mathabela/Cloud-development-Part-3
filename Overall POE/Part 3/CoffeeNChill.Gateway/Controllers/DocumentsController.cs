using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>Operational files for staff. Every endpoint needs an Admin or Staff token.</summary>
    [ApiController]
    [Route("api/documents")]
    [Authorize(Roles = "Admin,Staff")]
    public class DocumentsController : ControllerBase
    {
        private readonly FunctionsClient _functions;

        public DocumentsController(FunctionsClient functions)
        {
            _functions = functions;
        }

        /// <summary>Uploads a file to blob storage. Admin or Staff only.</summary>
        /// <param name="file">The file to upload.</param>
        /// <response code="200">The file was uploaded.</response>
        /// <response code="400">No file was sent or the file is empty.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file.Length == 0)
            {
                return BadRequest(new ErrorResponseDto
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "The file is empty."
                });
            }

            string fileName = Path.GetFileName(file.FileName);

            using var stream = file.OpenReadStream();
            await _functions.UploadDocumentAsync(fileName, stream);

            return Ok(new MessageResponseDto { Message = $"Uploaded: {fileName}" });
        }

        /// <summary>Lists all the files. Admin or Staff only.</summary>
        /// <response code="200">The file names.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDocuments()
        {
            return Ok(await _functions.GetDocumentsAsync());
        }

        /// <summary>Downloads a file. Admin or Staff only.</summary>
        /// <param name="fileName">The name of the file.</param>
        /// <response code="200">The file.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        /// <response code="404">The file does not exist.</response>
        [HttpGet("download/{fileName}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK, "application/octet-stream")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Download(string fileName)
        {
            var stream = await _functions.DownloadDocumentAsync(fileName);

            return File(stream, "application/octet-stream", fileName);
        }

        /// <summary>Deletes a file. Admin or Staff only.</summary>
        /// <param name="fileName">The name of the file.</param>
        /// <response code="200">The file was deleted.</response>
        /// <response code="401">No valid token was sent.</response>
        /// <response code="403">The user is not Admin or Staff.</response>
        /// <response code="404">The file does not exist.</response>
        [HttpDelete("{fileName}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string fileName)
        {
            await _functions.DeleteDocumentAsync(fileName);

            return Ok(new MessageResponseDto { Message = $"Deleted: {fileName}" });
        }
    }
}
