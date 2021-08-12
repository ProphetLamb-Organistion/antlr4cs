// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public class OptionalBlock : AltBlock
    {
        public OptionalBlock(OutputModelFactory factory,
            GrammarAST questionAST,
            IList<CodeBlockForAlt> alts)
            : base(factory, questionAST, alts)
        {
        }
    }
}