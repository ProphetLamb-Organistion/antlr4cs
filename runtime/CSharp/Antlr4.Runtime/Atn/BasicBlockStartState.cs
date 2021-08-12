// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    /// <author>Sam Harwell</author>
    public sealed class BasicBlockStartState : BlockStartState
    {
        public override StateType StateType => StateType.BlockStart;
    }
}