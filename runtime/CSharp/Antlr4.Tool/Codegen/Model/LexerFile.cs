// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;

namespace Antlr4.Codegen.Model
{
    public class LexerFile : OutputFile
    {
        public string exportMacro; // from -DexportMacro cmd-line
        public bool genListener; // from -listener cmd-line
        public string genPackage; // from -package cmd-line
        public bool genVisitor; // from -visitor cmd-line

        [ModelElement] public Lexer lexer;

        [ModelElement] public IDictionary<string, Action> namedActions;

        public LexerFile(OutputModelFactory factory, string fileName)
            : base(factory, fileName)
        {
            namedActions = BuildNamedActions(factory.GetGrammar());
            genPackage = factory.GetGrammar().tool.genPackage;
            exportMacro = factory.GetGrammar().GetOptionString("exportMacro");
            genListener = factory.GetGrammar().tool.gen_listener;
            genVisitor = factory.GetGrammar().tool.gen_visitor;
        }
    }
}