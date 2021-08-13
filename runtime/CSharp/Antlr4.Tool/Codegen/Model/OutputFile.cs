// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    public abstract class OutputFile : OutputModelObject
    {
        public readonly string ANTLRVersion;
        public readonly string fileName;
        public readonly string grammarFileName;
        public readonly string InputSymbolType;
        public readonly string TokenLabelType;

        protected OutputFile(OutputModelFactory factory, string fileName)
            : base(factory)
        {
            this.fileName = fileName;
            Grammar g = factory.GetGrammar();
            grammarFileName = g.fileName;
            ANTLRVersion = AntlrTool.VERSION;
            TokenLabelType = g.GetOptionString("TokenLabelType");
            InputSymbolType = TokenLabelType;
        }

        public virtual IDictionary<string, Action> BuildNamedActions(Grammar g)
        {
            IDictionary<string, Action> namedActions = new Dictionary<string, Action>();
            foreach (string name in g.namedActions.Keys)
            {
                ActionAST ast = g.namedActions[name];
                namedActions[name] = new Action(factory, ast);
            }

            return namedActions;
        }
    }
}