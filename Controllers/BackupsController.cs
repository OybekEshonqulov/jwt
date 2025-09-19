using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace jwtDocker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupsController : ControllerBase
    {
        private readonly string backupPath = "/var/backups/postgres/";

        // Himoyalangan endpoint (faqat Admin token bilan kirish mumkin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetBackups()
        {
            if (!Directory.Exists(backupPath))
                return NotFound("Backup papkasi topilmadi!");

            var files = Directory.GetFiles(backupPath, "*.sql")
                .Select(f => new
                {
                    FileName = Path.GetFileName(f),
                    SizeKB = Math.Round(new FileInfo(f).Length / 1024.0, 2),
                    LastModified = System.IO.File.GetLastWriteTime(f)
                })
                .OrderByDescending(f => f.LastModified)
                .ToList();

            return Ok(files);
        }
        /*ienduenfwfenuew*/
    }
}