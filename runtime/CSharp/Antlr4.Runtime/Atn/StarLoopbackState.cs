// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    public sealed class StarLoopbackState : ATNState
    {
        public StarLoopEntryState LoopEntryState => (StarLoopEntryState) Transition(0).target;

        public override StateType StateType => StateType.StarLoopBack;
    }
}