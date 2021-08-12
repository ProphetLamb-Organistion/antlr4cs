// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;

namespace Antlr4.Codegen.Model
{
    /**
     * All the rule elements we can label like tokens, rules, sets, wildcard.
     */
    public interface LabeledOp
    {
        IList<Decl.Decl> GetLabels();
    }
}