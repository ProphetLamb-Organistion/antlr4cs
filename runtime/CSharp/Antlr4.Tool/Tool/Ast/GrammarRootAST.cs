// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Antlr4.Tool.Ast
{
    public class GrammarRootAST : GrammarASTWithOptions
    {
        public static readonly IDictionary<string, string> defaultOptions = new Dictionary<string, string>
        {
            {"language", "Java"},
            {"abstract", "false"}
        };

        /**
         * Track stream used to create this tree
         */
        [NotNull] public readonly ITokenStream tokenStream;

        public IDictionary<string, string> cmdLineOptions; // -DsuperClass=T on command line
        public string fileName;

        public int grammarType; // LEXER, PARSER, GRAMMAR (combined)
        public bool hasErrors;

        public GrammarRootAST(GrammarRootAST node)
            : base(node)
        {
            grammarType = node.grammarType;
            hasErrors = node.hasErrors;
            tokenStream = node.tokenStream;
        }

        public GrammarRootAST(IToken t, ITokenStream tokenStream)
            : base(t)
        {
            if (tokenStream == null)
            {
                throw new ArgumentNullException(nameof(tokenStream));
            }

            this.tokenStream = tokenStream;
        }

        public GrammarRootAST(int type, IToken t, ITokenStream tokenStream)
            : base(type, t)
        {
            if (tokenStream == null)
            {
                throw new ArgumentNullException(nameof(tokenStream));
            }

            this.tokenStream = tokenStream;
        }

        public GrammarRootAST(int type, IToken t, string text, ITokenStream tokenStream)
            : base(type, t, text)
        {
            if (tokenStream == null)
            {
                throw new ArgumentNullException(nameof(tokenStream));
            }

            this.tokenStream = tokenStream;
        }

        public virtual string GetGrammarName()
        {
            ITree t = GetChild(0);
            if (t != null)
            {
                return t.Text;
            }

            return null;
        }

        public override string GetOptionString(string key)
        {
            if (cmdLineOptions != null && cmdLineOptions.ContainsKey(key))
            {
                return cmdLineOptions[key];
            }

            string value = base.GetOptionString(key);
            if (value == null)
            {
                defaultOptions.TryGetValue(key, out value);
            }

            return value;
        }

        public override object Visit(GrammarASTVisitor v)
        {
            return v.Visit(this);
        }

        public override ITree DupNode()
        {
            return new GrammarRootAST(this);
        }
    }
}