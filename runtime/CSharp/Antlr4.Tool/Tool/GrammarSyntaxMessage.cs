// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;

namespace Antlr4.Tool
{
    /**
     * A problem with the syntax of your antlr grammar such as
     * "The '{' came as a complete surprise to me at this point in your program"
     */
    public class GrammarSyntaxMessage : ANTLRMessage
    {
        public GrammarSyntaxMessage(ErrorType etype,
            string fileName,
            IToken offendingToken,
            RecognitionException antlrException,
            params object[] args)
            : base(etype, antlrException, offendingToken, args)
        {
            this.fileName = fileName;
            this.offendingToken = offendingToken;
            if (offendingToken != null)
            {
                line = offendingToken.Line;
                charPosition = offendingToken.CharPositionInLine;
            }
        }

        public new RecognitionException GetCause()
        {
            return (RecognitionException) base.GetCause();
        }
    }
}