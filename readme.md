# How to Keep Your Customers’ Phone Numbers Private in an Uber-for-X Solution


There are a number of use cases for number forwarding. Say, for instance, you want to have local presence in a country or a city and you want to protect your users’ privacy by not giving out or displaying their phone numbers. In this tutorial we will build a super simple API using C# that will redirect phone calls to a given number using our SVAML. We will use the caller id of the calling party to determine where we connect the call. 

As usual, you can find the full source code on [GitHub](https://github.com/sinch/net-redirect-call) or deploy directly to your Azure account if you want to try it out.

<a href="https://azuredeploy.net/?repository=https://github.com/sinch/net-redirect-call/" target="_blank">
    <img src="images/deploybutton.png"/>
</a>

The flow of the calls in this kind of solution looks like this:
<img src="images/napkin-diagram.png"/>
In this tutorial, we are going to implement the backend part.

## Prerequisites 
1. A [Sinch account](https://www.sinch.com/signup) and an app with keys 
2. A phone number [rented from Sinch] (https://www.sinch.com/dashboard/#/numbers); make sure it’s a voice number

## Configure your app 
Once you have a phone number—in my case, +1 213-454-0537—assign it to your app by clicking on the pen and then Voice: 
![configure your app](images/configureapp.png)
Enter a callbackurl. This is the URL the Sinch service will hit when there is a call happening associated with your app. 
 
## Code
This service is going to have two endpoints: one for associating a caller id with a specific phone number to call and one for the actual callback URL for the Sinch backend. 

###Configure API
In this tutorial, I am going to store the configuration in memory, so let’s create a simple model to keep track of the “from” and “to” numbers. 

```csharp
namespace Models {
    public class NumberConfigContext
    {
        private static List<NumberConfig> _numberConfigs;
        public static List<NumberConfig> Current()
        {
            if (_numberConfigs == null)
                _numberConfigs = new List<NumberConfig>();
            return _numberConfigs;
        }
    }
    public class NumberConfig {
        public string From { get; set; }
        public string To { get; set; }
    }
}
```

The above code just adds a static list with configs, where “from” is the calling phone and “to” is the phone to which we want it to be connected. I abstracted this for your benefit, so you can have an internal service like this that is entirely decoupled from your user database. 
Next, let’s add an endpoint in our WebAPI to configure where we want to connect.

**ConfigureController.cs**
```csharp
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
```

Let's build a list current configs to:
```csharp
[HttpGet]
public List<NumberConfig> Get() {
    return NumberConfigContext.Current();
}
```

Build and *post* to the url with i.e postman ([https://www.getpostman.com/](https://www.getpostman.com/)) 
http://yourserver/api/Configure?from=+15612600684&to=+460000000000
and then a *get* 
http://yourserver/api/Configure
and you should see that we successfully added a config. 

## Implementing the callback controller 
Create WebAPI controller called **SinchController**; this controller will be responsible for parsing and responding to SVAML. Sinch CTO Björn Fransson has been nice enough to make a NuGet with the all the SVAML we support, including some undocumented features—can you spot them? For a the list of supported SVAML, check out the [documentation](https://www.sinch.com/docs/voice/rest/#callbackapi "Callback documentation") if you prefer to make it yourself instead of NuGet.

```nugetgithub
install-package Sinch.Callback
```
And now the actual code:

```csharp
public Svamlet Post(CallbackEventModel model)
{
    var sinch = new CallbackFactory(new Locale("en-US")); //1
    Svamlet result = null;
    var builder = sinch.CreateIceSvamletBuilder();
    if (NumberConfigContext.Current().Any(c => c.From == model.From)) {
		//handle invalid configs, here you can also delete any configs if its supposed to be valid for one time only
        var config = NumberConfigContext.Current()
			.FirstOrDefault(c => c.From == model.From);
		//instruct sinch to connect the call with no further callbacks (ACE and DICE)
        result = builder.ConnectPstn(config.To)
				.WithCli(model.To.Endpoint)
				.WithoutCallbacks().Model;
    }
    else {
		// no config cound, tell the caller and hangup
        result = builder.Say("invalid caller id").Hangup().Model;
    }
    return result;
}
```

As you can see in the above code, it’s super simple to create some pretty nice functionality with just a few lines of code. Note that we are replacing the caller id with the number the user dialed; in my case, the LA number to keep both users’ numbers private.

## What’s next?
This tutorial relies on caller id, which can be a little flaky since the user might not display the caller id. In our experience, it still works well with Sinch and many of our customers rely on caller id for their solutions. Alternatively, you could rent multiple numbers from us (you can determine how many with the maximum concurrent number of calls you need to support) and connect to the right number using destination only.
