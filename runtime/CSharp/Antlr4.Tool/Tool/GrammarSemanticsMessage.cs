// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;

namespace Antlr4.Tool
{
    /**
     * A problem with the symbols and/or meaning of a grammar such as rule
     * redefinition. Any msg where we can point to a location in the grammar.
     */
    public class GrammarSemanticsMessage : ANTLRMessage
    {
        public GrammarSemanticsMessage(ErrorType etype,
            string fileName,
            IToken offendingToken,
            params object[] args)
            : base(etype, offendingToken, args)
        {
            this.fileName = fileName;
            if (offendingToken != null)
            {
                line = offendingToken.Line;
                charPosition = offendingToken.CharPositionInLine;
            }
        }
    }
}