// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Runtime.Atn
{
    /// <summary>The Tokens rule start state linking to each lexer rule start state</summary>
    public sealed class TokensStartState : DecisionState
    {
        public override StateType StateType => StateType.TokenStart;
    }
}