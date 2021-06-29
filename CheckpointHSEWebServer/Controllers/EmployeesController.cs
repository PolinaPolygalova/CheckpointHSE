using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CheckpointHSEWebServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeesController : ControllerBase
    {
        [HttpPost("RecognizePerson")]
        public async Task<string> RecognizePerson([FromForm] IFormFile file)
        {
            if (file is null || EmployeesInfo.IsTrained == false)
            {
                return string.Empty;
            }
            try
            {
                return await EmployeesInfo.IdentifyFacesAsync(file);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return string.Empty;
            }
        }
    }
}
