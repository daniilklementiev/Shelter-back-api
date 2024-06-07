using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Shelter.Models;
using Shelter.Services;

namespace Shelter.Controllers;

[ApiController]
[Route("[controller]")]
public class AdvtController : ControllerBase
{
    private readonly CosmosDbService _cosmosDbService;

    public AdvtController(CosmosDbService cosmosDbService)
    {
        _cosmosDbService = cosmosDbService;
    }

    [HttpGet]
    [Route("GetAllAdvts")]
    public async Task<IActionResult> GetAllAdvts()
    {
        var advts = (await _cosmosDbService.GetItemsAsync<Advt>("SELECT * FROM c WHERE c.partitionKey = 'advt'")).OrderBy(x => x.Date);
        return Ok(advts);
    }

    [HttpGet]
    [Route("GetAdvtById/{id}")]
    public async Task<IActionResult> GetAdvtById(String id)
    {
        var advts = await _cosmosDbService.GetItemsAsync<Advt>("SELECT * FROM c WHERE c.id = @id", ("@id", id));
        var advt = advts.FirstOrDefault();

        if (advt == null)
        {
            return NotFound();
        }

        return Ok(advt);
    }

    [HttpPost]
    [Route("AddAdvt")]
    public async Task<IActionResult> AddAdvt([FromBody] AdvtRequestModel model)
    {
        if (model == null)
        {
            return BadRequest("Invalid advt data received");
        }

        if (string.IsNullOrEmpty(model.Title) || string.IsNullOrEmpty(model.Description))
        {
            return BadRequest("Invalid advt data received");
        }

        var NewModel = new Advt
        {
            id = Guid.NewGuid().ToString(),
            partitionKey = "advt",
            Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            Image = null,
            AuthorId = model.AuthorId,
            Title = model.Title,
            Description = model.Description,
            Price = model.Price,
            Category = model.Category,
            City = model.City
        };


        var registrationTask = _cosmosDbService.AddItemAsync(NewModel); // adding the advt to the database
        registrationTask.Wait(); // waiting for the task to complete

        return CreatedAtAction(nameof(AddAdvt), NewModel);
    }

    [HttpDelete]
    [Route("DeleteAdvt/{id}")]
    public async Task<IActionResult> DeleteAdvt(String id)
    {
        var advts = await _cosmosDbService.GetItemsAsync<Advt>("SELECT * FROM c WHERE c.id = @id", ("@id", id));
        var advt = advts.FirstOrDefault();

        if (advt == null)
        {
            return NotFound();
        }

        await _cosmosDbService.DeleteItemAsync<Advt>(id.ToString(), advt);

        return NoContent();
    }

    [HttpPatch]
    [Route("UpdateAdvt/{id}")]
    public async Task<IActionResult> UpdateAdvt(String id, [FromBody] Advt advt)
    {
        if (id.ToString() != advt.id)
        {
            return BadRequest("Id in the path and in the request body do not match");
        }

        var existingAdvt = (await _cosmosDbService.GetItemsAsync<Advt>("SELECT * FROM c WHERE c.id = @id", ("@id", id))).FirstOrDefault();
        if (existingAdvt == null)
        {
            return NotFound("Advt not found");
        }

        existingAdvt.Title = advt.Title ?? existingAdvt.Title;
        existingAdvt.Description = advt.Description ?? existingAdvt.Description;
        existingAdvt.Price = advt.Price ?? existingAdvt.Price;
        existingAdvt.Category = advt.Category ?? existingAdvt.Category;
        existingAdvt.Image = advt.Image ?? existingAdvt.Image;

        await _cosmosDbService.UpdateItemAsync(id.ToString(), existingAdvt);

        return Ok("Advt successfully edited");
    }


       [HttpPost]
    [Route("UploadImage/{id}")]
    public async Task<IActionResult> UploadAvatar(string id, [FromForm] IFormFile file)
    {
        if (file == null)
        {
            return BadRequest(new { message = "No file received" });
        }

        var advt = (await _cosmosDbService.GetItemsAsync<Advt>($"SELECT * FROM c WHERE c.id = \"{id}\"")).FirstOrDefault();
        if (advt == null)
        {
            return NotFound("User not found");
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "Empty file received" });
        }

        if (file.Length > 1048576)
        {
            return BadRequest(new { message = "File size exceeds 1MB" });
        }

        var extension = Path.GetExtension(file.FileName);
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
        {
            return BadRequest(new { message = "Invalid file type. Only .jpg, .jpeg and .png are allowed" });
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine("D:\\home",  fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        advt.Image = fileName;
        await _cosmosDbService.UpdateItemAsync(id, advt);

        return Ok(new { message = "Avatar uploaded successfully" });
    }

    [HttpGet]
    [Route("GetImageByFilename")]
    public async Task<IActionResult> GetImageByFilename(string filename)
    {
        var filepath = Path.Combine("D:\\home", filename);
        if (!System.IO.File.Exists(filepath))
        {
            return NotFound("Image not found");
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filepath, out var mimeType))
        {
            mimeType = "application/octet-stream";
        }

        return File(System.IO.File.ReadAllBytes(filepath), mimeType);
    }

    
}