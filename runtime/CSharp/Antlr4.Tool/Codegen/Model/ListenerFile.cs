// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Antlr4.Misc;
using Antlr4.Runtime;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    /**
     * A model object representing a parse tree listener file.
     * These are the rules specific events triggered by a parse tree visitor.
     */
    public class ListenerFile : OutputFile
    {
        public string exportMacro; // from -DexportMacro cmd-line
        public string genPackage; // from -package cmd-line
        public string grammarName;

        [ModelElement] public Action header;

        /**
         * For listener contexts created for a labeled outer alternative, maps from
         * a listener context name to the name of the rule which defines the
         * context.
         */
        public IDictionary<string, string> listenerLabelRuleNames = new LinkedHashMap<string, string>();

        /**
         * The names of all listener contexts.
         */
        public ISet<string> listenerNames = new LinkedHashSet<string>();

        [ModelElement] public IDictionary<string, Action> namedActions;

        public string parserName;

        public ListenerFile(OutputModelFactory factory, string fileName)
            : base(factory, fileName)
        {
            Grammar g = factory.GetGrammar();
            parserName = g.GetRecognizerName();
            grammarName = g.name;

            namedActions = BuildNamedActions(factory.GetGrammar());

            foreach (KeyValuePair<string, IList<RuleAST>> entry in g.contextASTs)
            {
                foreach (RuleAST ruleAST in entry.Value)
                {
                    try
                    {
                        IDictionary<string, IList<Tuple<int, AltAST>>> labeledAlternatives = g.GetLabeledAlternatives(ruleAST);
                        listenerNames.UnionWith(labeledAlternatives.Keys);
                    }
                    catch (RecognitionException)
                    {
                    }
                }
            }

            foreach (Rule r in g.rules.Values)
            {
                listenerNames.Add(r.GetBaseContext());
            }

            foreach (Rule r in g.rules.Values)
            {
                IDictionary<string, IList<Tuple<int, AltAST>>> labels = r.GetAltLabels();
                if (labels != null)
                {
                    foreach (KeyValuePair<string, IList<Tuple<int, AltAST>>> pair in labels)
                    {
                        listenerLabelRuleNames[pair.Key] = r.name;
                    }
                }
            }

            ActionAST ast;
            if (g.namedActions.TryGetValue("header", out ast) && ast != null)
            {
                header = new Action(factory, ast);
            }

            genPackage = factory.GetGrammar().tool.genPackage;
            exportMacro = factory.GetGrammar().GetOptionString("exportMacro");
        }
    }
}