using System.Collections.Generic;

namespace FNB.InContact.Parser.FunctionApp.Models.BusMessages;

public class ReceivedSendGridEmailBusMessage
{
    public string RequestBody { get; set; }
    public bool RequestHasFormContentType { get; set; }
    public string RequestContentType { get; set; }
    public IDictionary<string, string> RequestHeaders { get; set; }

    public ReceivedSendGridEmailBusMessage(
        string requestBody,
        bool requestHasFormContentType,
        string requestContentType,
        IDictionary<string, string> requestHeaders)
    {
        RequestBody = requestBody;
        RequestHasFormContentType = requestHasFormContentType;
        RequestContentType = requestContentType;
        RequestHeaders = requestHeaders;
    }
}