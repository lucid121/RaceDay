using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services;

// The API talks to Azure through this service. The MVC app never talks to Azure.
public interface IBlobStorageService
{
    // Saves the image in Azure and returns its web address (URL)
    Task<string> UploadImageAsync(IFormFile file, string folder);
}

public class BlobStorageService : IBlobStorageService
{
    // We only accept these image types. The value is the file ending we save with.
    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        { "image/jpeg", ".jpg" },
        { "image/png", ".png" },
        { "image/webp", ".webp" }
    };

    public const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IConfiguration _config;

    public BlobStorageService(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        // Read the settings (the connection string is kept secret, NOT in GitHub)
        var connectionString = _config["AzureBlobStorage:ConnectionString"];
        var containerName = _config["AzureBlobStorage:ContainerName"] ?? "raceday-images";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        // Step 1: find the container (like a folder in the cloud). Create it if it is missing.
        // PublicAccessType.Blob = anyone with the link can VIEW the image (needed to show it on a web page).
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // Step 2: make a unique file name so two uploads never replace each other
        var extension = AllowedTypes[file.ContentType];
        var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";

        // Step 3: upload the file
        var blob = container.GetBlobClient(blobName);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        // Step 4: give back the web address
        return blob.Uri.ToString();
    }

    // Returns an error message if the file is not allowed, or null if the file is fine
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return "Please choose an image file.";
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            return "The image must be 5 MB or smaller.";
        }

        if (!AllowedTypes.ContainsKey(file.ContentType))
        {
            return "Only JPG, PNG or WEBP images are allowed.";
        }

        return null;
    }
}
