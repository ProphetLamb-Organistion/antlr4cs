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
    ///     This implementation of
    ///     <see cref="ILexerAction" />
    ///     is used for tracking input offsets
    ///     for position-dependent actions within a
    ///     <see cref="LexerActionExecutor" />
    ///     .
    ///     <p>
    ///         This action is not serialized as part of the ATN, and is only required for
    ///         position-dependent lexer actions which appear at a location other than the
    ///         end of a rule. For more information about DFA optimizations employed for
    ///         lexer actions, see
    ///         <see cref="LexerActionExecutor.Append(LexerActionExecutor, ILexerAction)" />
    ///         and
    ///         <see cref="LexerActionExecutor.FixOffsetBeforeMatch(int)" />
    ///         .
    ///     </p>
    /// </summary>
    /// <author>Sam Harwell</author>
    /// <since>4.2</since>
    public sealed class LexerIndexedCustomAction : ILexerAction
    {
        /// <summary>
        ///     Constructs a new indexed custom action by associating a character offset
        ///     with a
        ///     <see cref="ILexerAction" />
        ///     .
        ///     <p>
        ///         Note: This class is only required for lexer actions for which
        ///         <see cref="ILexerAction.IsPositionDependent()" />
        ///         returns
        ///         <see langword="true" />
        ///         .
        ///     </p>
        /// </summary>
        /// <param name="offset">
        ///     The offset into the input
        ///     <see cref="Antlr4.Runtime.ICharStream" />
        ///     , relative to
        ///     the token start index, at which the specified lexer action should be
        ///     executed.
        /// </param>
        /// <param name="action">
        ///     The lexer action to execute at a particular offset in the
        ///     input
        ///     <see cref="Antlr4.Runtime.ICharStream" />
        ///     .
        /// </param>
        public LexerIndexedCustomAction(int offset, [NotNull] ILexerAction action)
        {
            this.Offset = offset;
            this.Action = action;
        }

        /// <summary>
        ///     Gets the location in the input
        ///     <see cref="Antlr4.Runtime.ICharStream" />
        ///     at which the lexer
        ///     action should be executed. The value is interpreted as an offset relative
        ///     to the token start index.
        /// </summary>
        /// <returns>
        ///     The location in the input
        ///     <see cref="Antlr4.Runtime.ICharStream" />
        ///     at which the lexer
        ///     action should be executed.
        /// </returns>
        public int Offset { get; }

        /// <summary>Gets the lexer action to execute.</summary>
        /// <returns>
        ///     A
        ///     <see cref="ILexerAction" />
        ///     object which executes the lexer action.
        /// </returns>
        [NotNull]
        public ILexerAction Action { get; }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns the result of calling
        ///     <see cref="ActionType()" />
        ///     on the
        ///     <see cref="ILexerAction" />
        ///     returned by
        ///     <see cref="Action()" />
        ///     .
        /// </returns>
        public LexerActionType ActionType => Action.ActionType;

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see langword="true" />
        ///     .
        /// </returns>
        public bool IsPositionDependent => true;

        /// <summary>
        ///     <inheritDoc />
        ///     <p>
        ///         This method calls
        ///         <see cref="Execute(Antlr4.Runtime.Lexer)" />
        ///         on the result of
        ///         <see cref="Action()" />
        ///         using the provided
        ///         <paramref name="lexer" />
        ///         .
        ///     </p>
        /// </summary>
        public void Execute(Lexer lexer)
        {
            // assume the input stream position was properly set by the calling code
            Action.Execute(lexer);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Offset, ActionType);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is LexerIndexedCustomAction))
            {
                return false;
            }

            LexerIndexedCustomAction other = (LexerIndexedCustomAction) obj;
            return Offset == other.Offset && Action.Equals(other.Action);
        }
    }
}