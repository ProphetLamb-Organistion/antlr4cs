// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /** */
    public class MatchToken : RuleElement, LabeledOp
    {
        public IList<Decl.Decl> labels = new List<Decl.Decl>();
        public string name;
        public int ttype;

        public MatchToken(OutputModelFactory factory, TerminalAST ast)
            : base(factory, ast)
        {
            Grammar g = factory.GetGrammar();
            ttype = g.GetTokenType(ast.Text);
            name = factory.GetTarget().GetTokenTypeAsTargetLabel(g, ttype);
        }

        public MatchToken(OutputModelFactory factory, GrammarAST ast)
            : base(factory, ast)
        {
        }

        public virtual IList<Decl.Decl> GetLabels()
        {
            return labels;
        }
    }
}