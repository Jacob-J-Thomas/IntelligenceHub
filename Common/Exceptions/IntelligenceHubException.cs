﻿namespace IntelligenceHub.Common.Exceptions
{
    public class IntelligenceHubException : Exception
    {
        public IntelligenceHubException() { }

        public IntelligenceHubException(int statusCode, string? message = null) : base(message) 
        {
            StatusCode = statusCode;
        }

        public int StatusCode { get; set; }
    }
}