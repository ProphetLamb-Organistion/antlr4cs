// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    /// <summary>Mark the end of a * or + loop.</summary>
    public sealed class LoopEndState : ATNState
    {
        public ATNState loopBackState;

        public override StateType StateType => StateType.LoopEnd;
    }
}