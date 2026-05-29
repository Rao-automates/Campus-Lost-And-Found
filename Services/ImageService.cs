using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Services
{
    public interface IImageService
    {
        Task<List<ItemImage>> SaveItemImagesAsync(IEnumerable<IFormFile> files, int itemId, ApplicationDbContext db);
        Task<string> SaveAvatarAsync(IFormFile file, string userId);
        void DeleteFile(string relativePath);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public async Task<List<ItemImage>> SaveItemImagesAsync(IEnumerable<IFormFile> files, int itemId, ApplicationDbContext db)
        {
            var images = new List<ItemImage>();
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "items");
            Directory.CreateDirectory(uploadPath);

            bool isFirst = true;
            foreach (var file in files.Take(5)) // Max 5 files
            {
                if (file.Length == 0 || file.Length > MaxFileSize || !_allowedMimeTypes.Contains(file.ContentType))
                    continue;

                var guid = Guid.NewGuid().ToString();
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"{guid}{ext}";
                var thumbName = $"{guid}_thumb{ext}";

                var fullPath = Path.Combine(uploadPath, fileName);
                var thumbPath = Path.Combine(uploadPath, thumbName);

                using (var image = await Image.LoadAsync(file.OpenReadStream()))
                {
                    // Full image resize (max 1200x1200, preserve aspect ratio)
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(1200, 1200)
                    }));
                    await image.SaveAsync(fullPath);

                    // Thumbnail resize (center crop 300x300)
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Crop,
                        Size = new Size(300, 300)
                    }));
                    await image.SaveAsync(thumbPath);
                }

                var itemImage = new ItemImage
                {
                    ItemId = itemId,
                    Path = $"/uploads/items/{fileName}",
                    IsPrimary = isFirst,
                    UploadedAt = DateTime.UtcNow
                };

                images.Add(itemImage);
                db.ItemImages.Add(itemImage);
                isFirst = false;
            }

            await db.SaveChangesAsync();
            return images;
        }

        public async Task<string> SaveAvatarAsync(IFormFile file, string userId)
        {
            if (file.Length == 0 || file.Length > MaxFileSize || !_allowedMimeTypes.Contains(file.ContentType))
                throw new InvalidOperationException("Invalid image file.");

            var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadPath);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{userId}{ext}";
            var fullPath = Path.Combine(uploadPath, fileName);

            // Delete existing avatar if it exists (might be different extension, so just pattern match)
            var existingFiles = Directory.GetFiles(uploadPath, $"{userId}.*");
            foreach (var f in existingFiles)
            {
                File.Delete(f);
            }

            using (var image = await Image.LoadAsync(file.OpenReadStream()))
            {
                // Resize and center crop to 200x200
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(200, 200)
                }));
                await image.SaveAsync(fullPath);
            }

            return $"/uploads/avatars/{fileName}";
        }

        public void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
