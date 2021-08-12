// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    public class StarBlockAST : GrammarAST, RuleElementAST, QuantifierAST
    {
        private readonly bool _greedy;

        public StarBlockAST(StarBlockAST node)
            : base(node)
        {
            _greedy = node._greedy;
        }

        public StarBlockAST(int type, IToken t, IToken nongreedy)
            : base(type, t)
        {
            _greedy = nongreedy == null;
        }

        public virtual bool GetGreedy()
        {
            return _greedy;
        }

        public override ITree DupNode()
        {
            return new StarBlockAST(this);
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}