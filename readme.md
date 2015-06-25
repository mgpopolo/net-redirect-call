# How to keep your customers phone number private in an Uber for X solution


There is a number of use cases for number forwarding such as you want to have local presence in a country or a city. You want to protect your users privacy by not giving out or displaying their phone number. In this tutorial we are going to build a super simple API using c# where we redirect phone calls to a given number using our SVAML. 
In this tutorial we are going to use the callerid of the calling party to determine where we connect the call. 

As usual you can find the full source code on [GitHub](https://github.com/sinch/net-redirect-call) or deploy directly to to you azure account if you want to try it out.
<a href="https://azuredeploy.net/?repository=https://github.com/sinch/net-redirect-call" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

The flow of the calls in this kind of solution looks like this
<img src="http://www.websequencediagrams.com/files/render?link=pYJjAbI_xMYLY3GRziIW"/>
The part we are going to implement in this tutorial is the Backend Part

## Prerequisites 
1. Sinch account and an app with keys [Signup here ](https://www.sinch.com/signup)
2. A phone number rented from Sinch [rent one here](https://www.sinch.com/dashboard/#/numbers) make sure its a voice number.

## Configure your app 
Once you have a phone number in my case +1 213-454-0537 assign it to your app by clicking on the pen and then Voice: 
![](images/configureapp.png)
and enter a callbackurl, this is the URL the Sinch service will hit when there is a call happening associated to your app. 
 
## Code!
This service is going to have two endpoints, one for associating a caller id with a specific phone number to call and the actual callback URL for the Sinch backend. 

###Configure API
In this tutorial I am going to store the configuration in memory so lets create a simple model to keep track of from and to number. 

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

Sweet, The above code just adds a static list with configs, where from is the calling phone and to is where we want it to be connected. I abstracted this for yor benefit so you could have an internal service like this that is totaly decoupled from your user database (And I didnt have to build one for this demo;) ). 
Next lets add an endpoint in our WebAPI to configure where we want to connect

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
Lest build a list current configs to
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
Create WebAPI controller called **SinchController**, this controller will be responsible for parsing and responding to SVAML, Our CTO Bjorn Fransson has been good enough to make a nuget with the all the SVAML we support (including some undocumented features, can you spot them?). For a the list of supported SVAML check out the [documentation](https://www.sinch.com/docs/voice/rest/#callbackapi "Callback documentation") if you prefer to make it your self instead of nuget.

```nugetgithub
install-package Sinch.Callback
```
And now the actual code

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

As you can see in the above code its super simple to create some pretty nice functionality with just a few lines of code. Note that we are replacing the caller id with the number the user dialed in my case the LA number to keep both users number private for them.

## Whats next
This tutorial relies on callerid, this can be a littel bit flaky sinch the user might not display the caller id, in our experience it still works super well with Sinch and many of our customers rely on caller id for their solutions. Another way of implementing kind of the same functionality is to rent multiple numbers from us (how many you can determine with the maximum concurent number of calls you need to support) and connect to the right number using destination only. 


