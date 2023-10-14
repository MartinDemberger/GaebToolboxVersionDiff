using System;

namespace GaebToolBoxVersionCompare2
{
    public class ErrorDetails
    {
        public required string File { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FrameworkPath { get; set; }
        public string? CorePath { get; set; }
        public Exception? Exception { get; set; }
    }
}
