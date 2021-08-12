// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;

namespace Antlr4.Parse
{
    /** */
    public class v4ParserException : RecognitionException
    {
        public string msg;

        public v4ParserException(string msg, IIntStream input)
            : base(input)
        {
            this.msg = msg;
        }
    }
}