// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    public class ActionAST : GrammarASTWithOptions, RuleElementAST
    {
        public IList<IToken> chunks; // useful for ANTLR IDE developers

        // Alt, rule, grammar space
        public AttributeResolver resolver;

        public ActionAST(ActionAST node)
            : base(node)
        {
            resolver = node.resolver;
            chunks = node.chunks;
        }

        public ActionAST(IToken t)
            : base(t)
        {
        }

        public ActionAST(int type)
            : base(type)
        {
        }

        public ActionAST(int type, IToken t)
            : base(type, t)
        {
        }

        public override ITree DupNode()
        {
            return new ActionAST(this);
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }
    }
}