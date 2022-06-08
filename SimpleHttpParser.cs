using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Durar
{
    /// <summary>
    /// Simple HTTP Parser Class
    /// Coded By: Felipe Durar
    /// </summary>
    public class SimpleHttpParser
    {
        // Supports HTTP/1.x
        private readonly string[] HTTP_VERSIONS = new string[] { "HTTP/1.0", "HTTP/1.1" };
        private readonly string[] HTTP_METHODS = Enum.GetNames(typeof(HttpMethods));

        private List<byte> Buffer = new List<byte>();
        public Queue<HttpMessage> ParsedMessages { get; set; } = new Queue<HttpMessage>();

        private int CurrentPosition = 0;
        private Stack<int> PositionStack = new Stack<int>();

        private bool IsOutOfBounds() => CurrentPosition >= Buffer.Count;

        public bool IgnoreExceptions { get; set; } = true;

        /// <summary>
        /// Push Data into the Buffer and parse it (depending on the parseIt parameter)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="parseIt"></param>
        public void PushData(byte[] data, bool parseIt = true)
        {
            Buffer.AddRange(data);

            if (parseIt)
                Parse();
        }

        /// <summary>
        /// Try to parse the content on the buffer, any HTTP Message found and parsed is enqueued in the ParsedMessages Queue
        /// </summary>
        public void Parse()
        {
            try
            {
                while (true)
                {
                    HttpMessageType messageTypeFound = FindBeginOfMessage();

                    // Throws away useless content before the begin
                    Flush();

                    // Just ignore if no HTTP Message wasn't found
                    if (messageTypeFound == HttpMessageType.None)
                        break;

                    // If anything goes wrong or more TCP Packets are needed to complete a request it will return to this position for the next parse
                    int bkpPosition = CurrentPosition;

                    HttpMessage httpMessage = null;

                    if (messageTypeFound == HttpMessageType.Request)
                        httpMessage = ParseRequestLine();
                    else if (messageTypeFound == HttpMessageType.Response)
                        httpMessage = ParseResponseLine();

                    if (httpMessage == null)
                    {
                        // Matches the HTTP Version or HTTP Method here that was found on the FindBeginOfMessage call
                        // This is necessary to avoid infinite loops when any HTTP Version or Method is found without the full message
                        GetUntilSequence(" ", true, 10);
                        continue;
                    }
                    if (httpMessage.MessageType == HttpMessageType.None) continue; // Probably never reaches here (i hope)

                    // Try to parse the headers
                    if (!ParseHeaders(httpMessage))
                    {
                        if (IsOutOfBounds()) 
                        {
                            // Need more tcp packets to build the headers
                            Seek(bkpPosition);
                            break;
                        }
                        else
                            continue;   // Invalid header, just move on
                    }

                    // Check if the message contains content after the header
                    if (httpMessage.Headers.Any(cHeader => cHeader.Key == "Content-Length"))
                    {
                        KeyValuePair<string, string> contentLengthFound = httpMessage.Headers.Find(cHeader => cHeader.Key == "Content-Length");

                        int contentLength = Convert.ToInt32(contentLengthFound.Value);

                        httpMessage.Content = TryGetNBytes(contentLength);
                        if (httpMessage.Content == null)
                        {
                            // Need more tcp packets to build the headers
                            Seek(bkpPosition);
                            break;
                        }
                    }

                    ParsedMessages.Enqueue(httpMessage);

                    // Flushes the parsed content
                    Flush();
                }
            }
            catch (Exception ex)
            {
                if (IgnoreExceptions) 
                    Flush();    // This will throw away the bytes parsed till the exception
                else
                    throw ex;
            }

            PositionStack.Clear();
        }

        /// <summary>
        /// Removes all the bytes until the current position and resets the position to the begining of the list
        /// </summary>
        private void Flush()
        {
            int amountToRemove = CurrentPosition;
            if (amountToRemove >= Buffer.Count)
                amountToRemove = Buffer.Count - 1;
            if (amountToRemove > 0)
            {
                Buffer.RemoveRange(0, amountToRemove);
                Seek(0);
            }
        }

        /// <summary>
        /// Sets the current position
        /// </summary>
        /// <param name="position"></param>
        private void Seek(int position) { CurrentPosition = position; }

        /// <summary>
        /// Finds any content in the buffer that begins with a HTTP Version or a HTTP Method,
        /// HTTP Requests begins with the name of the HTTP Method,
        /// HTTP Responses begins with the HTTP Version
        /// </summary>
        /// <returns></returns>
        private HttpMessageType FindBeginOfMessage()
        {
            while (true)
            {
                int bkpPosition = CurrentPosition;
                string content = GetUntilSequence(" ", true, 10);
                Seek(bkpPosition);

                if (HTTP_METHODS.Contains(content)) // e.g.         GET / HTTP/1.1[CR][LF]
                    return HttpMessageType.Request;
                else if (HTTP_VERSIONS.Contains(content)) // e.g.   HTTP/1.1 200 OK[CR][LF]
                    return HttpMessageType.Response;
                else
                {
                    // If is not the begin of a request or a response just go to the next character
                    byte cByte = GetByte();
                    if (IsOutOfBounds())
                        return HttpMessageType.None;
                }
            }
        }

        /// <summary>
        /// Try to parse the first line of a HTTP Request,
        /// If it is an invalid HTTP Request it returns null
        /// </summary>
        /// <returns></returns>
        private HttpMessage ParseRequestLine()
        {
            // e.g. HTTP Request Line
            // GET / HTTP/1.1[CR][LF]

            int bkpPosition = CurrentPosition;

            string method = string.Empty;
            string route = string.Empty;
            string httpVer = string.Empty;

            method = GetUntilSequence(" ", true, 10);
            if (!HTTP_METHODS.Contains(method))
            {
                Seek(bkpPosition);
                return null;
            }

            route = GetUntilSequence(" ");

            httpVer = GetUntilSequence("\r\n", true, 10);
            if (!HTTP_VERSIONS.Contains(httpVer))
            {
                Seek(bkpPosition);
                return null;
            }

            // Create the HTTP Message
            HttpMessage httpPacket = new HttpMessage();
            httpPacket.HttpVersion = httpVer;
            httpPacket.Method = (HttpMethods)Enum.Parse(typeof(HttpMethods), method, true);
            httpPacket.Route = route;
            httpPacket.MessageType = HttpMessageType.Request;
            return httpPacket;
        }

        /// <summary>
        /// Try to parse the first line of a HTTP Response,
        /// If it is an invalid HTTP Response it returns null
        /// </summary>
        /// <returns></returns>
        private HttpMessage ParseResponseLine()
        {
            // e.g. HTTP Request Line
            // HTTP/1.1 200 OK[CR][LF]

            int bkpPosition = CurrentPosition;

            string httpVer = string.Empty;
            int statusCode = -1;
            string statusText = string.Empty;

            httpVer = GetUntilSequence(" ", true, 10);
            if (!HTTP_VERSIONS.Contains(httpVer))
            {
                Seek(bkpPosition);
                return null;
            }

            string tmpStatusCode = GetUntilSequence(" ", true, 10);
            if (tmpStatusCode == null)
            {
                Seek(bkpPosition);
                return null;
            }
            if (!int.TryParse(tmpStatusCode, out statusCode))
            {
                Seek(bkpPosition);
                return null;
            }

            statusText = GetUntilSequence("\r\n", true, 50);

            // Create the HTTP Message
            HttpMessage httpPacket = new HttpMessage();
            httpPacket.HttpVersion = httpVer;
            httpPacket.StatusCode = statusCode;
            httpPacket.StatusText = statusText;
            httpPacket.MessageType = HttpMessageType.Response;
            return httpPacket;
        }

        /// <summary>
        /// Try to parse the HTTP Headers,
        /// Returns false if the header is invalid
        /// </summary>
        /// <param name="httpPacket"></param>
        /// <returns></returns>
        private bool ParseHeaders(HttpMessage httpPacket)
        {
            // The headers are key value pairs separated by colon
            // It ends when it matches double CRLF ([CR][LF][CR][LF])
            // e.g. HTTP Headers
            // Host: foo.com[CR][LF]
            // Content - Type: application / x - www - form - urlencoded[CR][LF]
            // Content - Length: 13[CR][LF]
            // [CR][LF]

            string allHeaders = GetUntilSequence("\r\n\r\n", true);
            if (allHeaders == null) return false;
            string[] headers = allHeaders.Split(new[] { '\r', '\n' });
            foreach (string cHeader in headers)
            {
                if (cHeader.IndexOf(':') != -1)
                {
                    string[] headerParts = cHeader.Split(':');
                    httpPacket.Headers.Add(new KeyValuePair<string, string>(headerParts[0], headerParts[1]));
                }
                else { /* WTF?? */ }
            }

            return true;
        }

        /// <summary>
        /// Get the byte at the current position and goes to the next position
        /// </summary>
        /// <returns></returns>
        private byte GetByte()
        {
            if (IsOutOfBounds()) return 0;
            byte cByte = Buffer[CurrentPosition];
            Seek(CurrentPosition + 1);
            return cByte;
        }

        /// <summary>
        /// Get a string until a given sequence is found
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="matchSequence"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private string GetUntilSequence(string sequence, bool matchSequence = true, int maxLength = -1)
        {
            byte[] byteSequence = Encoding.UTF8.GetBytes(sequence);
            byte[] sequenceFound = GetUntilSequence(byteSequence, matchSequence, maxLength);
            if (sequenceFound == null) return null;
            return Encoding.UTF8.GetString(sequenceFound);
        }

        /// <summary>
        /// Get the bytes until a given sequence is found
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="matchSequence"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private byte[] GetUntilSequence(byte[] sequence, bool matchSequence = true, int maxLength = -1)
        {
            int bkpPosition = CurrentPosition;
            List<byte> seqFound = new List<byte>();
            for (int c = 0; true; c++)
            {
                bool isMatch = PrevMatchSequence(sequence);
                if (isMatch)
                {
                    if (matchSequence) MatchSequence(sequence);
                    break;
                }


                byte cByte = GetByte();
                if (cByte == 0)
                {
                    Seek(bkpPosition);
                    return null;
                }
                seqFound.Add(cByte);

                if (maxLength != -1 && c > maxLength)
                    break;
            }
            return seqFound.ToArray();
        }

        /// <summary>
        /// Get a N amount of bytes from the current position,
        /// Returns null if it reaches the end of the buffer before the given amount
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private byte[] TryGetNBytes(int amount)
        {
            List<byte> seqFound = new List<byte>();
            for (int c = 0; c < amount; c++)
            {
                byte cByte = GetByte();
                if (IsOutOfBounds())
                    return null;

                seqFound.Add(cByte);
            }
            return seqFound.ToArray();
        }

        /// <summary>
        /// Matches a sequence without changing the current position
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private bool PrevMatchSequence(byte[] sequence)
        {
            int bkpPosition = CurrentPosition;
            bool isMatch = MatchSequence(sequence);
            Seek(bkpPosition);
            return isMatch;
        }

        /// <summary>
        /// Matches a sequence
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        private bool MatchSequence(byte[] sequence)
        {
            bool different = false;
            foreach (byte cByte in sequence)
            {
                byte nextByte = GetByte();
                if (cByte != nextByte)
                {
                    different = true;
                    break;
                }
            }
            return !different;
        }

    }

    public class HttpMessage
    {
        public string HttpVersion { get; set; } = string.Empty;
        public HttpMessageType MessageType { get; set; } = HttpMessageType.None;
        public HttpMethods Method { get; set; } = HttpMethods.None;
        public string Route { get; set; } = string.Empty;
        public List<KeyValuePair<string, string>> Headers { get; set; } = new List<KeyValuePair<string, string>>();

        public byte[] Content { get; set; } = new byte[0];
        public int StatusCode { get; set; } = 0;
        public string StatusText { get; set; } = string.Empty;

        public string GetContentAsString() => Encoding.UTF8.GetString(Content);
    }

    public enum HttpMessageType
    {
        None, Request, Response
    }

    public enum HttpMethods
    {
        None, GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE, PATCH
    }
}
