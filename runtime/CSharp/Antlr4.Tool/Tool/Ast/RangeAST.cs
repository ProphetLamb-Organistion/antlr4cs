// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    public class RangeAST : GrammarAST, RuleElementAST
    {
        public RangeAST(RangeAST node)
            : base(node)
        {
        }

        public RangeAST(IToken t)
            : base(t)
        {
        }

        public override ITree DupNode()
        {
            return new RangeAST(this);
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}