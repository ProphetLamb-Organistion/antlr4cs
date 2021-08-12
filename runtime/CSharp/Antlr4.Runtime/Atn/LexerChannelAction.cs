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
    ///     Implements the
    ///     <c>channel</c>
    ///     lexer action by calling
    ///     <see cref="Lexer.Channel" />
    ///     with the assigned channel.
    /// </summary>
    /// <author>Sam Harwell</author>
    /// <since>4.2</since>
    public sealed class LexerChannelAction : ILexerAction
    {
        /// <summary>
        ///     Constructs a new
        ///     <paramref name="channel" />
        ///     action with the specified channel value.
        /// </summary>
        /// <param name="channel">
        ///     The channel value to pass to
        ///     <see cref="Lexer.Channel" />
        ///     .
        /// </param>
        public LexerChannelAction(int channel)
        {
            this.Channel = channel;
        }

        /// <summary>
        ///     Gets the channel to use for the
        ///     <see cref="Antlr4.Runtime.IToken" />
        ///     created by the lexer.
        /// </summary>
        /// <returns>
        ///     The channel to use for the
        ///     <see cref="Antlr4.Runtime.IToken" />
        ///     created by the lexer.
        /// </returns>
        public int Channel { get; }

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see cref="LexerActionType.Channel" />
        ///     .
        /// </returns>
        public LexerActionType ActionType => LexerActionType.Channel;

        /// <summary>
        ///     <inheritDoc />
        /// </summary>
        /// <returns>
        ///     This method returns
        ///     <see langword="false" />
        ///     .
        /// </returns>
        public bool IsPositionDependent => false;

        /// <summary>
        ///     <inheritDoc />
        ///     <p>
        ///         This action is implemented by calling
        ///         <see cref="Lexer.Channel" />
        ///         with the
        ///         value provided by
        ///         <see cref="Channel()" />
        ///         .
        ///     </p>
        /// </summary>
        public void Execute([NotNull] Lexer lexer)
        {
            lexer.Channel = Channel;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, Channel);
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is LexerChannelAction))
            {
                return false;
            }

            return Channel == ((LexerChannelAction) obj).Channel;
        }

        public override string ToString()
        {
            return $"channel({Channel})";
        }
    }
}