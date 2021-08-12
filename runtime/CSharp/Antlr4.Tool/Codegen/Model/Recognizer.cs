// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Codegen.Model.Chunk;
using Antlr4.Misc;
using Antlr4.Tool;

namespace Antlr4.Codegen.Model
{
    public abstract class Recognizer : OutputModelObject
    {
        public bool abstractRecognizer;

        [ModelElement] public SerializedATN atn;

        public string grammarFileName;
        public string grammarName;

        public IList<string> literalNames;
        public string name;
        public ICollection<string> ruleNames;
        public ICollection<Rule> rules;

        [ModelElement] public LinkedHashMap<Rule, RuleSempredFunction> sempredFuncs =
            new();

        [ModelElement] public ActionChunk superClass;

        public IList<string> symbolicNames;

        /**
         * @deprecated This field is provided only for compatibility with code
         * generation targets which have not yet been updated to use
         * {@link #literalNames} and {@link #symbolicNames}.
         */
        [Obsolete] public IList<string> tokenNames;

        public IDictionary<string, int> tokens;

        protected Recognizer(OutputModelFactory factory)
            : base(factory)
        {
            Grammar g = factory.GetGrammar();
            grammarFileName = Path.GetFileName(g.fileName);
            grammarName = g.name;
            name = g.GetRecognizerName();
            tokens = new LinkedHashMap<string, int>();
            foreach (KeyValuePair<string, int> entry in g.tokenNameToTypeMap)
            {
                int ttype = entry.Value;
                if (ttype > 0)
                {
                    tokens[entry.Key] = ttype;
                }
            }

            ruleNames = g.rules.Keys;
            rules = g.rules.Values;
            atn = new SerializedATN(factory, g.atn, g.GetRuleNames());
            if (g.GetOptionString("superClass") != null)
            {
                superClass = new ActionText(null, g.GetOptionString("superClass"));
            }
            else
            {
                superClass = null;
            }

#pragma warning disable CS0612 // Type or member is obsolete
            tokenNames = TranslateTokenStringsToTarget(g.GetTokenDisplayNames(), factory);
#pragma warning restore CS0612 // Type or member is obsolete
            literalNames = TranslateTokenStringsToTarget(g.GetTokenLiteralNames(), factory);
            symbolicNames = TranslateTokenStringsToTarget(g.GetTokenSymbolicNames(), factory);
            abstractRecognizer = g.IsAbstract();
        }

        protected static IList<string> TranslateTokenStringsToTarget(string[] tokenStrings, OutputModelFactory factory)
        {
            string[] result = (string[]) tokenStrings.Clone();
            for (int i = 0;
                i < tokenStrings.Length;
                i++)
            {
                result[i] = TranslateTokenStringToTarget(tokenStrings[i], factory);
            }

            int lastTrueEntry = result.Length - 1;
            while (lastTrueEntry >= 0 && result[lastTrueEntry] == null)
            {
                lastTrueEntry--;
            }

            if (lastTrueEntry < result.Length - 1)
            {
                Array.Resize(ref result, lastTrueEntry + 1);
            }

            return result;
        }

        protected static string TranslateTokenStringToTarget(string tokenName, OutputModelFactory factory)
        {
            if (tokenName == null)
            {
                return null;
            }

            if (tokenName[0] == '\'')
            {
                bool addQuotes = false;
                string targetString =
                    factory.GetTarget().GetTargetStringLiteralFromANTLRStringLiteral(factory.GetGenerator(), tokenName, addQuotes);
                return "\"'" + targetString + "'\"";
            }

            return factory.GetTarget().GetTargetStringLiteralFromString(tokenName, true);
        }
    }
}