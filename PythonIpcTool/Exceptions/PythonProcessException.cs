// File: PythonIpcTool/Exceptions/PythonProcessException.cs
using System;

namespace PythonIpcTool.Exceptions
{
    /// <summary>
    /// Custom exception for errors related to starting or communicating with the Python process.
    /// </summary>
    public class PythonProcessException : Exception
    {
        public PythonProcessException(string message) : base(message)
        {
        }

        public PythonProcessException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}