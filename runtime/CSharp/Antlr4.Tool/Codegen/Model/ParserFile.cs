// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Codegen.Model.Chunk;
using Antlr4.Tool;

namespace Antlr4.Codegen.Model
{
    /** */
    public class ParserFile : OutputFile
    {
        [ModelElement] public ActionChunk contextSuperClass;

        public string exportMacro; // from -DexportMacro cmd-line
        public bool genListener; // from -listener cmd-line
        public string genPackage; // from -package cmd-line
        public bool genVisitor; // from -visitor cmd-line
        public string grammarName;

        [ModelElement] public IDictionary<string, Action> namedActions;

        [ModelElement] public Parser parser;

        public ParserFile(OutputModelFactory factory, string fileName)
            : base(factory, fileName)
        {
            Grammar g = factory.GetGrammar();
            namedActions = BuildNamedActions(factory.GetGrammar());
            genPackage = g.tool.genPackage;
            exportMacro = factory.GetGrammar().GetOptionString("exportMacro");
            // need the below members in the ST for Python, C++
            genListener = g.tool.gen_listener;
            genVisitor = g.tool.gen_visitor;
            grammarName = g.name;

            if (g.GetOptionString("contextSuperClass") != null)
            {
                contextSuperClass = new ActionText(null, g.GetOptionString("contextSuperClass"));
            }
        }
    }
}