// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Codegen.Model.Decl;

namespace Antlr4.Codegen.Model.Chunk
{
    /** */
    public class TokenPropertyRef : ActionChunk
    {
        public string label;

        public TokenPropertyRef(StructDecl ctx, string label)
            : base(ctx)
        {
            this.label = label;
        }
    }
}