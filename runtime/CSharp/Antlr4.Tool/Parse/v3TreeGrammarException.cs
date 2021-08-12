// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime;
using Antlr4.Runtime.Exceptions;

namespace Antlr4.Parse
{
    public class v3TreeGrammarException : ParseCanceledException
    {
        public IToken location;

        public v3TreeGrammarException(IToken location)
        {
            this.location = location;
        }
    }
}