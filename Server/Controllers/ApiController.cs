using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace Server.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private static readonly HashSet<string> SupportedExtensions = new() { ".png", ".jpg", ".jpeg" };
    private static readonly string SaveDir = "../tmp";

    private void push_to_queue(string taskGuid, string extension)
    {
        ConnectionFactory factory = new() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(
            queue: "task_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicPublish(
            exchange: "",
            routingKey: "task_queue",
            basicProperties: null,
            body: Encoding.UTF8.GetBytes($"{taskGuid}{extension}"));
    }
    
    // POST /api/upload
    /// <summary>
    /// Puts image in queue
    /// </summary>
    /// <param name="image"></param>
    /// <returns>Task GUID</returns>
    /// <response code="201">The file is valid, file saved, task scheduled</response>
    /// <response code="400">The file have to have only [.png, .jpg and .jpeg] extention</response>            
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    public ActionResult<string> Upload([Required] IFormFile image)
    {
        var extension = Path.GetExtension(image.FileName);
        if (!SupportedExtensions.Contains(extension))
        {
            return BadRequest(
                $"Extension {extension} not supported. Expected one of {string.Join(" ", SupportedExtensions.ToList())}");
        }

        // generate guid
        var taskGuid = Guid.NewGuid().ToString();

        // save file
        var imagePath = Path.Combine(SaveDir, $"{taskGuid}{extension}");
        var statusPath = Path.Combine(SaveDir, $"{taskGuid}_status.txt");

        using (var stream = System.IO.File.Create(imagePath))
        {
            image.CopyTo(stream);
        }

        using (var fs = new FileStream(statusPath, FileMode.Create, FileAccess.Write))
        {
            fs.Write(Encoding.UTF8.GetBytes("Pending"));
        }

        // push filename to queue
        push_to_queue(taskGuid, extension);

        return taskGuid;
    }

    // GET /api/{task_id}/status
    /// <summary>
    /// Checks status of GUID task
    /// </summary>
    /// <param name="taskGuid"></param>
    /// <returns>Status of task if it's OK, or NotFound exception otherwise</returns>
    /// <response code="200">The task was found, the status is returned</response>
    /// <response code="404">The task was not found</response>      
    [HttpPost("{taskGuid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public ActionResult<string> Status(string taskGuid)
    {
        var statusFilePath = Path.Combine(SaveDir, $"{taskGuid}_status.txt");
        if (!System.IO.File.Exists(statusFilePath))
        {
            return NotFound($"No task with {taskGuid} found");
        }
        
        return Ok(System.IO.File.ReadAllText(statusFilePath));
    }

    //GET /api/{task_id}/download            
    /// <summary>
    /// Gets a caption of task's image, deletes files with status and result
    /// </summary>
    /// <param name="taskGuid"></param>
    /// <returns>Caption of task if it's OK or NoTFound exception otherwise</returns>
    /// <response code="200">The task was found, the status is returned, files deleted</response>
    /// <response code="404">The task was not found</response>   
    [HttpPost("{taskGuid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
    public ActionResult<string> Download(string taskGuid)
    {
        var resultFilePath = Path.Combine(SaveDir, $"{taskGuid}_result.txt");
        if (!System.IO.File.Exists(resultFilePath))
        {
            return NotFound($"No task with {taskGuid} found");
        }
        var result = System.IO.File.ReadAllText(resultFilePath);
        System.IO.File.Delete(resultFilePath);
        System.IO.File.Delete(Path.Combine(SaveDir, $"{taskGuid}_status.txt"));
        return Ok(result);
    }
}