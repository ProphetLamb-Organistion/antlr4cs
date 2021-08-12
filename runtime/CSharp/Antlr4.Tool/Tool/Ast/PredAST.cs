// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    public class PredAST : ActionAST
    {
        public PredAST(PredAST node)
            : base(node)
        {
        }

        public PredAST(IToken t)
            : base(t)
        {
        }

        public PredAST(int type)
            : base(type)
        {
        }

        public PredAST(int type, IToken t)
            : base(type, t)
        {
        }

        public override ITree DupNode()
        {
            return new PredAST(this);
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}