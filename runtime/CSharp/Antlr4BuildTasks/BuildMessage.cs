// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Antlr4.Build.Tasks
{
    [Serializable]
    internal struct BuildMessage
    {
        private static readonly Regex BuildMessageFormat =
            new(@"^\s*(?<SEVERITY>[a-z]+)\((?<CODE>[0-9]+)\):\s*((?<FILE>.*):(?<LINE>[0-9]+):(?<COLUMN>[0-9]+):)?\s*(?:syntax error:\s*)?(?<MESSAGE>.*)$", RegexOptions.Compiled);

        public BuildMessage(string message)
            : this(TraceLevel.Error, message, null, 0, 0)
        {
            try
            {
                Match match = BuildMessageFormat.Match(message);
                if (match.Success)
                {
                    FileName = match.Groups["FILE"].Length > 0 ? match.Groups["FILE"].Value : "";
                    LineNumber = match.Groups["LINE"].Length > 0 ? Int32.Parse(match.Groups["LINE"].Value) : 0;
                    ColumnNumber = match.Groups["COLUMN"].Length > 0 ? Int32.Parse(match.Groups["COLUMN"].Value) + 1 : 0;

                    switch (match.Groups["SEVERITY"].Value)
                    {
                        case "warning":
                            Severity = TraceLevel.Warning;
                            break;
                        case "error":
                            Severity = TraceLevel.Error;
                            break;
                        default:
                            Severity = TraceLevel.Info;
                            break;
                    }

                    int code = Int32.Parse(match.Groups["CODE"].Value);
                    Message = $"AC{code:0000}: {match.Groups["MESSAGE"].Value}";
                }
                else
                {
                    Message = message;
                }
            }
            catch (Exception ex)
            {
                if (Antlr4ClassGenerationTask.IsFatalException(ex))
                {
                    throw;
                }
            }
        }

        public BuildMessage(TraceLevel severity, string message, string fileName, int lineNumber, int columnNumber)
            : this()
        {
            Severity = severity;
            Message = message;
            FileName = fileName;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public TraceLevel Severity { get; set; }

        public string Message { get; set; }

        public string FileName { get; set; }

        public int LineNumber { get; set; }

        public int ColumnNumber { get; set; }
    }
}