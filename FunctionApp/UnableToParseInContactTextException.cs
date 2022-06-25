using System;
using System.Runtime.Serialization;

namespace FNB.InContact.Parser.FunctionApp;

[Serializable]
public class UnableToParseInContactTextException : Exception
{
    public UnableToParseInContactTextException()
    {
    }

    public UnableToParseInContactTextException(string message)
        : base(message)
    {
    }

    public UnableToParseInContactTextException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected UnableToParseInContactTextException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context)
    {
    }
}