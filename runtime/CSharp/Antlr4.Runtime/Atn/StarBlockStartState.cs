// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    /// <summary>The block that begins a closure loop.</summary>
    public sealed class StarBlockStartState : BlockStartState
    {
        public override StateType StateType => StateType.StarBlockStart;
    }
}