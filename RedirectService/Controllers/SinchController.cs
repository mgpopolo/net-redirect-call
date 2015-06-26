using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using RedirectService.Models;
using Sinch.Callback;
using Sinch.Callback.Model;
using Sinch.Callback.Response;

namespace RedirectService.Controllers
{
    public class SinchController : ApiController
    {
        [HttpPost]
public Svamlet Post(CallbackEventModel model)
{
    var sinch = new CallbackFactory(new Locale("en-US"));
    Svamlet result = null;
    var builder = sinch.CreateIceSvamletBuilder();
    if (NumberConfigContext.Current().Any(c => c.From == model.From)) {
        var config = NumberConfigContext.Current().FirstOrDefault(c => c.From == model.From);
        result = builder.ConnectPstn(config.To).WithCli(model.To.Endpoint).WithoutCallbacks().Model;
    }
    else {
        result = builder.Say("Invalid caller id!").Hangup().Model;
    }
    return result;
}
    }
}
