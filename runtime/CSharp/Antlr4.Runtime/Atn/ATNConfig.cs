// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;
using System.Text;

namespace Antlr4.Runtime.Atn
{
    /// <summary>A tuple: (ATN state, predicted alt, syntactic, semantic context).</summary>
    /// <remarks>
    ///     A tuple: (ATN state, predicted alt, syntactic, semantic context).
    ///     The syntactic context is a graph-structured stack node whose
    ///     path(s) to the root is the rule invocation(s)
    ///     chain used to arrive at the state.  The semantic context is
    ///     the tree of semantic predicates encountered before reaching
    ///     an ATN state.
    /// </remarks>
    public class ATNConfig
    {
        /// <summary>
        ///     This field stores the bit mask for implementing the
        ///     <see cref="PrecedenceFilterSuppressed()" />
        ///     property as a bit within the
        ///     existing
        ///     <see cref="altAndOuterContextDepth" />
        ///     field.
        /// </summary>
        private const int SuppressPrecedenceFilter = unchecked((int) 0x80000000);

        /// <summary>The ATN state associated with this configuration</summary>
        [NotNull] private readonly ATNState state;

        /// <summary>This is a bit-field currently containing the following values.</summary>
        /// <remarks>
        ///     This is a bit-field currently containing the following values.
        ///     <ul>
        ///         <li>0x00FFFFFF: Alternative</li>
        ///         <li>0x7F000000: Outer context depth</li>
        ///         <li>0x80000000: Suppress precedence filter</li>
        ///     </ul>
        /// </remarks>
        private int altAndOuterContextDepth;

        /// <summary>
        ///     The stack of invoking states leading to the rule/states associated
        ///     with this config.
        /// </summary>
        /// <remarks>
        ///     The stack of invoking states leading to the rule/states associated
        ///     with this config.  We track only those contexts pushed during
        ///     execution of the ATN simulator.
        /// </remarks>
        [NotNull] private PredictionContext context;

        protected internal ATNConfig([NotNull] ATNState state, int alt, [NotNull] PredictionContext context)
        {
            Debug.Assert((alt & 0xFFFFFF) == alt);
            this.state = state;
            altAndOuterContextDepth = alt;
            this.context = context;
        }

        protected internal ATNConfig([NotNull] ATNConfig c, [NotNull] ATNState state, [NotNull] PredictionContext context)
        {
            this.state = state;
            altAndOuterContextDepth = c.altAndOuterContextDepth;
            this.context = context;
        }

        /// <summary>Gets the ATN state associated with this configuration.</summary>
        public ATNState State => state;

        /// <summary>What alt (or lexer rule) is predicted by this configuration.</summary>
        public int Alt => altAndOuterContextDepth & 0x00FFFFFF;

        public virtual PredictionContext Context
        {
            get => context;
            set
            {
                PredictionContext context = value;
                this.context = context;
            }
        }

        public bool ReachesIntoOuterContext => OuterContextDepth != 0;

        /// <summary>
        ///     We cannot execute predicates dependent upon local context unless
        ///     we know for sure we are in the correct context.
        /// </summary>
        /// <remarks>
        ///     We cannot execute predicates dependent upon local context unless
        ///     we know for sure we are in the correct context. Because there is
        ///     no way to do this efficiently, we simply cannot evaluate
        ///     dependent predicates unless we are in the rule that initially
        ///     invokes the ATN simulator.
        ///     <p>
        ///         closure() tracks the depth of how far we dip into the outer context:
        ///         depth &gt; 0.  Note that it may not be totally accurate depth since I
        ///         don't ever decrement. TODO: make it a boolean then
        ///     </p>
        /// </remarks>
        public virtual int OuterContextDepth
        {
            get => (int) ((uint) altAndOuterContextDepth >> 24) & 0x7F;
            set
            {
                int outerContextDepth = value;
                Debug.Assert(outerContextDepth >= 0);
                // saturate at 0x7F - everything but zero/positive is only used for debug information anyway
                outerContextDepth = Math.Min(outerContextDepth, 0x7F);
                altAndOuterContextDepth = (outerContextDepth << 24) | (altAndOuterContextDepth & ~0x7F000000);
            }
        }

        public virtual LexerActionExecutor ActionExecutor => null;

        public virtual SemanticContext SemanticContext => SemanticContext.None;

        public virtual bool PassedThroughNonGreedyDecision => false;

        public bool PrecedenceFilterSuppressed
        {
            get => (altAndOuterContextDepth & SuppressPrecedenceFilter) != 0;
            set
            {
                if (value)
                {
                    altAndOuterContextDepth |= SuppressPrecedenceFilter;
                }
                else
                {
                    altAndOuterContextDepth &= ~SuppressPrecedenceFilter;
                }
            }
        }

        public static ATNConfig Create([NotNull] ATNState state, int alt, [AllowNull] PredictionContext context)
        {
            return Create(state, alt, context, SemanticContext.None, null);
        }

        public static ATNConfig Create([NotNull] ATNState state, int alt, [AllowNull] PredictionContext context, [NotNull] SemanticContext semanticContext)
        {
            return Create(state, alt, context, semanticContext, null);
        }

        public static ATNConfig Create([NotNull] ATNState state, int alt, [AllowNull] PredictionContext context, [NotNull] SemanticContext semanticContext,
            LexerActionExecutor lexerActionExecutor)
        {
            if (semanticContext != SemanticContext.None)
            {
                if (lexerActionExecutor != null)
                {
                    return new ActionSemanticContextATNConfig(lexerActionExecutor, semanticContext, state, alt, context, false);
                }

                return new SemanticContextATNConfig(semanticContext, state, alt, context);
            }

            if (lexerActionExecutor != null)
            {
                return new ActionATNConfig(lexerActionExecutor, state, alt, context, false);
            }

            return new ATNConfig(state, alt, context);
        }

        public ATNConfig Clone()
        {
            return Transform(State, false);
        }

        public ATNConfig Transform([NotNull] ATNState state, bool checkNonGreedy)
        {
            return Transform(state, context, SemanticContext, checkNonGreedy, ActionExecutor);
        }

        public ATNConfig Transform([NotNull] ATNState state, [NotNull] SemanticContext semanticContext, bool checkNonGreedy)
        {
            return Transform(state, context, semanticContext, checkNonGreedy, ActionExecutor);
        }

        public ATNConfig Transform([NotNull] ATNState state, [AllowNull] PredictionContext context, bool checkNonGreedy)
        {
            return Transform(state, context, SemanticContext, checkNonGreedy, ActionExecutor);
        }

        public ATNConfig Transform([NotNull] ATNState state, LexerActionExecutor lexerActionExecutor, bool checkNonGreedy)
        {
            return Transform(state, context, SemanticContext, checkNonGreedy, lexerActionExecutor);
        }

        private ATNConfig Transform([NotNull] ATNState state, [AllowNull] PredictionContext context, [NotNull] SemanticContext semanticContext, bool checkNonGreedy,
            LexerActionExecutor lexerActionExecutor)
        {
            bool passedThroughNonGreedy = checkNonGreedy && CheckNonGreedyDecision(this, state);
            if (semanticContext != SemanticContext.None)
            {
                if (lexerActionExecutor != null || passedThroughNonGreedy)
                {
                    return new ActionSemanticContextATNConfig(lexerActionExecutor, semanticContext, this, state, context, passedThroughNonGreedy);
                }

                return new SemanticContextATNConfig(semanticContext, this, state, context);
            }

            if (lexerActionExecutor != null || passedThroughNonGreedy)
            {
                return new ActionATNConfig(lexerActionExecutor, this, state, context, passedThroughNonGreedy);
            }

            return new ATNConfig(this, state, context);
        }

        private static bool CheckNonGreedyDecision(ATNConfig source, ATNState target)
        {
            return source.PassedThroughNonGreedyDecision || target is DecisionState && ((DecisionState) target).nonGreedy;
        }

        public virtual ATNConfig AppendContext(int context, PredictionContextCache contextCache)
        {
            PredictionContext appendedContext = Context.AppendContext(context, contextCache);
            ATNConfig result = Transform(State, appendedContext, false);
            return result;
        }

        public virtual ATNConfig AppendContext(PredictionContext context, PredictionContextCache contextCache)
        {
            PredictionContext appendedContext = Context.AppendContext(context, contextCache);
            ATNConfig result = Transform(State, appendedContext, false);
            return result;
        }

        public virtual bool Contains(ATNConfig subconfig)
        {
            if (State.stateNumber != subconfig.State.stateNumber || Alt != subconfig.Alt || !SemanticContext.Equals(subconfig.SemanticContext))
            {
                return false;
            }

            var leftWorkList = new Stack<PredictionContext>();
            var rightWorkList = new Stack<PredictionContext>();
            leftWorkList.Push(Context);
            rightWorkList.Push(subconfig.Context);
            while (leftWorkList.Count > 0)
            {
                PredictionContext left = leftWorkList.Pop();
                PredictionContext right = rightWorkList.Pop();
                if (left == right)
                {
                    return true;
                }

                if (left.Size < right.Size)
                {
                    return false;
                }

                if (right.IsEmpty)
                {
                    return left.HasEmpty;
                }

                for (int i = 0;
                    i < right.Size;
                    i++)
                {
                    int index = left.FindReturnState(right.GetReturnState(i));
                    if (index < 0)
                    {
                        // assumes invokingStates has no duplicate entries
                        return false;
                    }

                    leftWorkList.Push(left.GetParent(index));
                    rightWorkList.Push(right.GetParent(i));
                }
            }

            return false;
        }

        /// <summary>
        ///     An ATN configuration is equal to another if both have
        ///     the same state, they predict the same alternative, and
        ///     syntactic/semantic contexts are the same.
        /// </summary>
        public override bool Equals(object o)
        {
            if (!(o is ATNConfig))
            {
                return false;
            }

            return Equals((ATNConfig) o);
        }

        public virtual bool Equals(ATNConfig other)
        {
            if (this == other)
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return State.stateNumber == other.State.stateNumber && Alt == other.Alt && ReachesIntoOuterContext == other.ReachesIntoOuterContext && Context.Equals(other.Context) &&
                   SemanticContext.Equals(other.SemanticContext) && PrecedenceFilterSuppressed == other.PrecedenceFilterSuppressed &&
                   PassedThroughNonGreedyDecision == other.PassedThroughNonGreedyDecision &&
                   EqualityComparer<LexerActionExecutor>.Default.Equals(ActionExecutor, other.ActionExecutor);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(State.stateNumber, Alt, ReachesIntoOuterContext ? 1 : 0, Context, SemanticContext, PassedThroughNonGreedyDecision ? 1 : 0, ActionExecutor);
        }

        public virtual string ToDotString()
        {
#if COMPACT
            throw new NotImplementedException("The current platform does not provide RuntimeHelpers.GetHashCode(object).");
#else
            StringBuilder builder = new();
            builder.Append("digraph G {\n");
            builder.Append("rankdir=LR;\n");
            var visited = new HashSet<PredictionContext>();
            var workList = new Stack<PredictionContext>();
            workList.Push(Context);
            visited.Add(Context);
            while (workList.Count > 0)
            {
                PredictionContext current = workList.Pop();
                for (int i = 0;
                    i < current.Size;
                    i++)
                {
                    builder.Append("  s").Append(RuntimeHelpers.GetHashCode(current));
                    builder.Append("->");
                    builder.Append("s").Append(RuntimeHelpers.GetHashCode(current.GetParent(i)));
                    builder.Append("[label=\"").Append(current.GetReturnState(i)).Append("\"];\n");
                    if (visited.Add(current.GetParent(i)))
                    {
                        workList.Push(current.GetParent(i));
                    }
                }
            }

            builder.Append("}\n");
            return builder.ToString();
#endif
        }

        public override string ToString()
        {
            return ToString(null, true, false);
        }

        public virtual string ToString(IRecognizer recog, bool showAlt)
        {
            return ToString(recog, showAlt, true);
        }

        public virtual string ToString(IRecognizer recog, bool showAlt, bool showContext)
        {
            StringBuilder buf = new();
            //		if ( state.ruleIndex>=0 ) {
            //			if ( recog!=null ) buf.append(recog.getRuleNames()[state.ruleIndex]+":");
            //			else buf.append(state.ruleIndex+":");
            //		}
            string[] contexts;
            if (showContext)
            {
                contexts = Context.ToStrings(recog, State.stateNumber);
            }
            else
            {
                contexts = new[] {"?"};
            }

            bool first = true;
            foreach (string contextDesc in contexts)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    buf.Append(", ");
                }

                buf.Append('(');
                buf.Append(State);
                if (showAlt)
                {
                    buf.Append(",");
                    buf.Append(Alt);
                }

                if (Context != null)
                {
                    buf.Append(",");
                    buf.Append(contextDesc);
                }

                if (SemanticContext != null && SemanticContext != SemanticContext.None)
                {
                    buf.Append(",");
                    buf.Append(SemanticContext);
                }

                if (ReachesIntoOuterContext)
                {
                    buf.Append(",up=").Append(OuterContextDepth);
                }

                buf.Append(')');
            }

            return buf.ToString();
        }

        private class SemanticContextATNConfig : ATNConfig
        {
            public SemanticContextATNConfig(SemanticContext semanticContext, [NotNull] ATNState state, int alt, [AllowNull] PredictionContext context)
                : base(state, alt, context)
            {
                SemanticContext = semanticContext;
            }

            public SemanticContextATNConfig(SemanticContext semanticContext, [NotNull] ATNConfig c, [NotNull] ATNState state, [AllowNull] PredictionContext context)
                : base(c, state, context)
            {
                SemanticContext = semanticContext;
            }

            [field: NotNull] public override SemanticContext SemanticContext { get; }
        }

        private class ActionATNConfig : ATNConfig
        {
            public ActionATNConfig(LexerActionExecutor lexerActionExecutor, [NotNull] ATNState state, int alt, [AllowNull] PredictionContext context,
                bool passedThroughNonGreedyDecision)
                : base(state, alt, context)
            {
                ActionExecutor = lexerActionExecutor;
                PassedThroughNonGreedyDecision = passedThroughNonGreedyDecision;
            }

            protected internal ActionATNConfig(LexerActionExecutor lexerActionExecutor, [NotNull] ATNConfig c, [NotNull] ATNState state, [AllowNull] PredictionContext context,
                bool passedThroughNonGreedyDecision)
                : base(c, state, context)
            {
                if (c.SemanticContext != SemanticContext.None)
                {
                    throw new NotSupportedException();
                }

                ActionExecutor = lexerActionExecutor;
                PassedThroughNonGreedyDecision = passedThroughNonGreedyDecision;
            }

            public override LexerActionExecutor ActionExecutor { get; }

            public override bool PassedThroughNonGreedyDecision { get; }
        }

        private class ActionSemanticContextATNConfig : SemanticContextATNConfig
        {
            public ActionSemanticContextATNConfig(LexerActionExecutor lexerActionExecutor, [NotNull] SemanticContext semanticContext, [NotNull] ATNState state, int alt,
                [AllowNull] PredictionContext context, bool passedThroughNonGreedyDecision)
                : base(semanticContext, state, alt, context)
            {
                ActionExecutor = lexerActionExecutor;
                PassedThroughNonGreedyDecision = passedThroughNonGreedyDecision;
            }

            public ActionSemanticContextATNConfig(LexerActionExecutor lexerActionExecutor, [NotNull] SemanticContext semanticContext, [NotNull] ATNConfig c,
                [NotNull] ATNState state, [AllowNull] PredictionContext context, bool passedThroughNonGreedyDecision)
                : base(semanticContext, c, state, context)
            {
                ActionExecutor = lexerActionExecutor;
                PassedThroughNonGreedyDecision = passedThroughNonGreedyDecision;
            }

            public override LexerActionExecutor ActionExecutor { get; }

            public override bool PassedThroughNonGreedyDecision { get; }
        }
    }
}