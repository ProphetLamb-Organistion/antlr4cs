// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    public sealed class RuleStartState : ATNState
    {
        public bool isPrecedenceRule;

        public bool leftFactored;
        public RuleStopState stopState;

        public override StateType StateType => StateType.RuleStart;
    }
}