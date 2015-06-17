using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using RedirectService.Models;

namespace RedirectService.Controllers {
    public class ConfigureController : ApiController {
[HttpPost]
public void Post(string from, string to) {
    // make sure we dont have duplicates
    NumberConfigContext.Current().RemoveAll(n => n.From == from);
    var config = new NumberConfig {
        From = from,
        To = to
    };
    NumberConfigContext.Current().Add(config);
}

[HttpGet]
public List<NumberConfig> Get() {
    return NumberConfigContext.Current();
}


    }
}
