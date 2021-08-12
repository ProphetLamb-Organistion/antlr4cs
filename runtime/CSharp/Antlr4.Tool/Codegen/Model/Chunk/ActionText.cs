// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Codegen.Model.Decl;

namespace Antlr4.Codegen.Model.Chunk
{
    /** */
    public class ActionText : ActionChunk
    {
        public string text;

        public ActionText(StructDecl ctx, string text)
            : base(ctx)
        {
            this.text = text;
        }
    }
}