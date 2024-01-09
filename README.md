# OpenAICustomFunctionCallingAPI\

This is a custom API designed to simplify consumption of the OpenAI endpoint, and create a methodology for connecting a virtually limitless amount of functions to call to a single request using the OpenAI service's function calling feature.\

This is accomplished by using a single function definition which then calls more specialized configurations that can gather additional data from the user if needed, and contain a larger variety of function definitions. This reduces costs by only providing function definitions when necessary (which consume tokens), and through triaging, can introduce as many functions as needed.\

Please add your own API token in appsettings.json after cloning the repo.\
You can use the below request body for testing:

```
{
  "prompt": "hello, can you please turn on my lights for me?",
  "functionEndpoint": "string",
  "functionNames": {
    "callHomeAutomationModel": "A function that should be used when the user wants to trigger, configure, or inquire about their home automations, such as lighting orchestrations, heating/cooling, etc.",
    "callEmailOrTextWriterModel": "A function that should be used when the user wants to write and send an email or a text message.",
    "callOrderPlacementModel": "A function that should be used when the user wants to place an order online. This can be food, amazon orders, or any other transaction else that can be done online."
  }
}
```
