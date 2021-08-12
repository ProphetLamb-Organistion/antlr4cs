// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public class ThrowRecognitionException : SrcOp
    {
        public int decision;
        public int grammarCharPosInLine;
        public string grammarFile;
        public int grammarLine;

        public ThrowRecognitionException(OutputModelFactory factory, GrammarAST ast, IntervalSet expecting)
            : base(factory, ast)
        {
            //this.decision = ((BlockStartState)ast.ATNState).decision;
            grammarLine = ast.Line;
            grammarLine = ast.CharPositionInLine;
            grammarFile = factory.GetGrammar().fileName;
            //this.expecting = factory.createExpectingBitSet(ast, decision, expecting, "error");
            //		factory.defineBitSet(this.expecting);
        }
    }
}