# Simple Http Parser
A Simple Single File HTTP Parser for C#.

## How to use it?
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

## License

MIT License

Copyright (c) 2022 Felipe Durar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
