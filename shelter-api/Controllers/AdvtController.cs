using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.Cosmos;
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
            AnimalType = model.AnimalType,
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
        existingAdvt.AnimalType = advt.AnimalType ?? existingAdvt.AnimalType;
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
            return NotFound("Advt not found");
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

    [HttpPost]
    [Route("SaveAdvt")]
    public async Task<IActionResult> SaveAdvt([FromBody] SavedAdvtsRequestModel advt)
    {
        if (advt == null)
        {
            return BadRequest("Invalid advt data received");
        }

        if(advt.AdvtId == null)
        {
            return BadRequest("Invalid advt data received");
        }

        if(advt.UserId == null)
        {
            return BadRequest("Unauthenticated user");
        }

        var newModel = new SavedAvts
        {
            id = Guid.NewGuid().ToString(),
            advtId = advt.AdvtId,
            userId = advt.UserId
        };

        var registrationTask = _cosmosDbService.AddItemAsync(newModel); // adding the advt to the database
        registrationTask.Wait(); // waiting for the task to complete

        return CreatedAtAction(nameof(SaveAdvt), newModel);
    }

    [HttpDelete]
    [Route("DeleteSavedAdvt/{id}")]
    public async Task<IActionResult> DeleteSavedAdvt(String id)
    {
        var savedAdvts = await _cosmosDbService.GetItemsAsync<SavedAvts>("SELECT * FROM c WHERE c.id = @id", ("@id", id));
        var savedAdvt = savedAdvts.FirstOrDefault();

        if (savedAdvt == null)
        {
            return NotFound();
        }

        await _cosmosDbService.DeleteItemAsync<SavedAvts>(id.ToString(), savedAdvt);

        return NoContent();
    }


    [HttpGet]
    [Route("GetSavedAdvts/{userId}")]
    public async Task<IActionResult> GetSavedAdvts(String userId)
    {
        var savedAdvts = await _cosmosDbService.GetItemsAsync<SavedAvts>("SELECT * FROM c WHERE c.userId = @userId", ("@userId", userId));
        if(savedAdvts == null)
        {
            return NotFound("No saved advts found");
        }
        return Ok(savedAdvts);
    }


    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? title = null,
        [FromQuery] string? category = null,
        [FromQuery] string? animalType = null,
        [FromQuery] string? city = null,
        [FromQuery] string? date = null
    )
    {
        var query = "SELECT * FROM c WHERE c.partitionKey = 'advt'";
        var parameters = new Dictionary<string, object>();
        bool hasParameters = false;

        if (!string.IsNullOrEmpty(title))
        {
            query += " AND CONTAINS(LOWER(c.Title), LOWER(@title))";
            parameters.Add("@title", title);
            hasParameters = true;
        }

        if (!string.IsNullOrEmpty(category))
        {
            query += " AND LOWER(c.Category) = LOWER(@category)";
            parameters.Add("@category", category);
            hasParameters = true;
        }

        if (!string.IsNullOrEmpty(animalType))
        {
            query += " AND LOWER(c.AnimalType) = LOWER(@animalType)";
            parameters.Add("@animalType", animalType);
            hasParameters = true;
        }

        if (!string.IsNullOrEmpty(city))
        {
            query += " AND LOWER(c.City) = LOWER(@city)";
            parameters.Add("@city", city);
            hasParameters = true;
        }

        if (!string.IsNullOrEmpty(date))
        {
            query += " AND LOWER(c.Date) = LOWER(@date)";
            parameters.Add("@date", date);
            hasParameters = true;
        }

        if (!hasParameters)
        {
            return NotFound("No search parameters provided");
        }

        var queryDefinition = new QueryDefinition(query);
        foreach (var parameter in parameters)
        {
            queryDefinition.WithParameter(parameter.Key, parameter.Value);
        }

        var container = _cosmosDbService.GetContainer();

        try
        {
            var resultSet = container.GetItemQueryIterator<Advt>(queryDefinition);
            var results = new List<Advt>();

            while (resultSet.HasMoreResults)
            {
                var response = await resultSet.ReadNextAsync();
                results.AddRange(response);
            }
            if(results.Count == 0)
            {
                return NotFound("No results found");
            }

            return Ok(results);
        }
        catch (CosmosException ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }

    [HttpPost]
    [Route("CreateResume")]
    public async Task<IActionResult> CreateResume(ResumeRequestModel model)
    {
        if (model == null)
        {
            return BadRequest("Invalid advt data received");
        }

        if (string.IsNullOrEmpty(model.Title) || string.IsNullOrEmpty(model.Description) || string.IsNullOrEmpty(model.Category) || string.IsNullOrEmpty(model.City) || string.IsNullOrEmpty(model.AnimalType) || string.IsNullOrEmpty(model.Price))
        {
            return BadRequest("Invalid resume data received");
        }

        var NewModel = new Resume()
        {
            id = Guid.NewGuid().ToString(),
            AuthorId = model.AuthorId,
            Title = model.Title,
            Description = model.Description,
            Price = model.Price,
            Category = model.Category,
            AnimalType = model.AnimalType,
            City = model.City
        };


        var registrationTask = _cosmosDbService.AddItemAsync(NewModel); // adding the advt to the database
        registrationTask.Wait(); // waiting for the task to complete

        return CreatedAtAction(nameof(AddAdvt), NewModel);
    }

    [HttpGet]
    [Route("GetResumeById/{id}")]
    public async Task<IActionResult> GetResumeById(String id)
    {
        var resumes = await _cosmosDbService.GetItemsAsync<Resume>("SELECT * FROM c WHERE c.AuthorId = @id", ("@id", id));
        var resume = resumes.FirstOrDefault();

        if (resume == null)
        {
            return NotFound();
        }

        return Ok(resume);
    }

    [HttpDelete]
    [Route("DeleteResume/{id}")]
    public async Task<IActionResult> DeleteResume(String id)
    {
        var resumes = await _cosmosDbService.GetItemsAsync<Resume>("SELECT * FROM c WHERE c.id = @id", ("@id", id));
        var resume = resumes.FirstOrDefault();

        if (resume == null)
        {
            return NotFound();
        }

        await _cosmosDbService.DeleteItemAsync<Resume>(id.ToString(), resume);

        return NoContent();
    }

    [HttpPatch]
    [Route("UpdateResume/{id}")]
    public async Task<IActionResult> UpdateResume(String id, [FromBody] Resume resume)
    {
        if (id.ToString() != resume.id)
        {
            return BadRequest("Id in the path and in the request body do not match");
        }

        var existingAdvt = (await _cosmosDbService.GetItemsAsync<Resume>("SELECT * FROM c WHERE c.id = @id", ("@id", id))).FirstOrDefault();
        if (existingAdvt == null)
        {
            return NotFound("Resume not found");
        }

        existingAdvt.Title = resume.Title ?? existingAdvt.Title;
        existingAdvt.Description = resume.Description ?? existingAdvt.Description;
        existingAdvt.Price = resume.Price ?? existingAdvt.Price;
        existingAdvt.AnimalType = resume.AnimalType ?? existingAdvt.AnimalType;
        existingAdvt.Category = resume.Category ?? existingAdvt.Category;

        await _cosmosDbService.UpdateItemAsync(id.ToString(), existingAdvt);

        return Ok("Resume successfully edited");
    }


}