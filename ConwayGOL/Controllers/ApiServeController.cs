using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace ConwayGOL.Controllers
{    
    class ApiServeController : ApiController
    {
        [HttpGet]
        [Route("GetCurrentGame")]
        public IHttpActionResult GetCurrentGameData()
        {
            var data = "this is a test";


            return Ok(data);
        }
    }
}





