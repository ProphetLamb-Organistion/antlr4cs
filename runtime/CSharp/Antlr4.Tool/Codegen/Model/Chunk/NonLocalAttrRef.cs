// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Codegen.Model.Decl;

namespace Antlr4.Codegen.Model.Chunk
{
    public class NonLocalAttrRef : ActionChunk
    {
        public string name;
        public int ruleIndex;
        public string ruleName;

        public NonLocalAttrRef(StructDecl ctx, string ruleName, string name, int ruleIndex)
            : base(ctx)
        {
            this.name = name;
            this.ruleName = ruleName;
            this.ruleIndex = ruleIndex;
        }
    }
}