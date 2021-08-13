// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.


using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    public class RuleRefAST : GrammarASTWithOptions, RuleElementAST
    {
        public RuleRefAST(RuleRefAST node)
            : base(node)
        {
        }

        public RuleRefAST(IToken t)
            : base(t)
        {
        }

        public RuleRefAST(int type)
            : base(type)
        {
        }

        public RuleRefAST(int type, IToken t)
            : base(type, t)
        {
        }

        /**
         * Dup token too since we overwrite during LR rule transform
         */
        public override ITree DupNode()
        {
            RuleRefAST r = new(this);
            // In LR transform, we alter original token stream to make e -> e[n]
            // Since we will be altering the dup, we need dup to have the
            // original token.  We can set this tree (the original) to have
            // a new token.
            r.Token = Token;
            Token = new CommonToken(r.Token);
            return r;
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}