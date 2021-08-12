// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif



namespace Antlr4.Runtime.Atn
{
    /// <summary>
    ///     Executes a custom lexer action by calling
    ///     <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
    ///     with the
    ///     rule and action indexes assigned to the custom action. The implementation of
    ///     a custom action is added to the generated code for the lexer in an override
    ///     of
    ///     <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
    ///     when the grammar is compiled.
    ///     <p>
    ///         This class may represent embedded actions created with the <code>{...}</code>
    ///         syntax in ANTLR 4, as well as actions created for lexer commands where the
    ///         command argument could not be evaluated when the grammar was compiled.
    ///     </p>
    /// </summary>
    /// <author>Sam Harwell</author>
    /// <since>4.2</since>
    public sealed class LexerCustomAction : ILexerAction
    {
        /// <summary>
        ///     Constructs a custom lexer action with the specified rule and action
        ///     indexes.
        /// </summary>
        /// <param name="ruleIndex">
        ///     The rule index to use for calls to
        ///     <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
        ///     .
        /// </param>
        /// <param name="actionIndex">
        ///     The action index to use for calls to
        ///     <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
        ///     .
        /// </param>
        public LexerCustomAction(int ruleIndex, int actionIndex)
        {
            this.RuleIndex = ruleIndex;
            this.ActionIndex = actionIndex;
        }

        /// <summary>
        ///     Gets the rule index to use for calls to
        ///     <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
        ///     .
        /// </summary>
        /// <returns>The rule index for the custom action.</returns>
        public int RuleIndex { get; }

        /// <summary>
        ///     Gets the action index to use for calls to
        ///     <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
        ///     .
        /// </summary>
        /// <returns>The action index for the custom action.</returns>
        public int ActionIndex { get; }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see cref="LexerActionType.Custom" />
        ///     .
        /// </returns>
        public LexerActionType ActionType => LexerActionType.Custom;

        /// <summary>Gets whether the lexer action is position-dependent.</summary>
        /// <remarks>
        ///     Gets whether the lexer action is position-dependent. Position-dependent
        ///     actions may have different semantics depending on the
        ///     <see cref="Antlr4.Runtime.ICharStream" />
        ///     index at the time the action is executed.
        ///     <p>
        ///         Custom actions are position-dependent since they may represent a
        ///         user-defined embedded action which makes calls to methods like
        ///         <see cref="Antlr4.Runtime.Lexer.Text()" />
        ///         .
        ///     </p>
        /// </remarks>
        /// <returns>
        ///     This method returns
        ///     <see langword="true" />
        ///     .
        /// </returns>
        public bool IsPositionDependent => true;

        /// <summary>
        ///     <inheritDoc />
        ///     <p>
        ///         Custom actions are implemented by calling
        ///         <see cref="Antlr4.Runtime.Recognizer{Symbol, ATNInterpreter}.Action(Antlr4.Runtime.RuleContext, int, int)" />
        ///         with the
        ///         appropriate rule and action indexes.
        ///     </p>
        /// </summary>
        public void Execute([NotNull] Lexer lexer)
        {
            lexer.Action(null, RuleIndex, ActionIndex);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, RuleIndex, ActionIndex);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is LexerCustomAction))
            {
                return false;
            }

            LexerCustomAction other = (LexerCustomAction) obj;
            return RuleIndex == other.RuleIndex && ActionIndex == other.ActionIndex;
        }
    }
}