using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CheckpointHSEWebServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeesController : ControllerBase
    {
        [HttpPost("RecognizePersons")]
        public async Task<IEnumerable<string>> RecognizePersons([FromForm] IFormFile file)
        {
            return await EmployeesInfo.IdentifyFacesAsync(file);
        }
    }
}
