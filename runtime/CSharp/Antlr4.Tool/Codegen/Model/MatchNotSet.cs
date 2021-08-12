// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Tool.Ast;

namespace Antlr4.Codegen.Model
{
    public class MatchNotSet : MatchSet
    {
        public string varName = "_la";

        public MatchNotSet(OutputModelFactory factory, GrammarAST ast)
            : base(factory, ast)
        {
        }
    }
}