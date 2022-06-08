# Simple Http Parser
A Simple Single File HTTP Parser for C#.

## How to user it?
Just copy the SimpleHttpParser.cs file into your project and import it :)

## Sample
```
using Durar;
...
SimpleHttpParser simpleHttpParser = new SimpleHttpParser();

// You can call PushData multiple times using the same object, some messages may require multiple tcp packets to be complete...
simpleHttpParser.PushData(... your incoming tcp data goes here ...);

// Any parsed message is available at ParsedMessages property
while (simpleHttpParser.ParsedMessages.Count > 0)
{
    HttpMessage httpMessage = simpleHttpParser.ParsedMessages.Dequeue();

    // httpMessage.MessageType -> Request or Response
    // httpMessage.Method      -> The HTTP Method (Only for Requests)
    // httpMessage.Route       -> The Route (Only for Requests)
    // httpMessage.StatusCode  -> The Status Code (Only for Response)
    // httpMessage.StatusText  -> The Status Text (Only for Response)
    // httpMessage.HttpVersion -> The HTTP Version from the HTTP Message
    // httpMessage.Headers     -> A List<KeyValuePair<string, string>> of all headers from the current request
    // httpMessage.Content     -> The Payload from the Request or the Response (Only available when the Content-Length header is present)
    // httpMessage.GetContentAsString() -> Function to get the payload as string
}
```
