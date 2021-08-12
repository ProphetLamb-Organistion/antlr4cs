// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public class ThrowNoViableAlt : ThrowRecognitionException
    {
        public ThrowNoViableAlt(OutputModelFactory factory, GrammarAST blkOrEbnfRootAST,
            IntervalSet expecting)
            : base(factory, blkOrEbnfRootAST, expecting)
        {
        }
    }
}