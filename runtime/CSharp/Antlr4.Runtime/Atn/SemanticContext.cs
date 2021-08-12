// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Atn
{
    /// <summary>
    ///     A tree structure used to record the semantic context in which
    ///     an ATN configuration is valid.
    /// </summary>
    /// <remarks>
    ///     A tree structure used to record the semantic context in which
    ///     an ATN configuration is valid.  It's either a single predicate,
    ///     a conjunction
    ///     <c>p1&amp;&amp;p2</c>
    ///     , or a sum of products
    ///     <c>p1||p2</c>
    ///     .
    ///     <p>
    ///         I have scoped the
    ///         <see cref="AND" />
    ///         ,
    ///         <see cref="OR" />
    ///         , and
    ///         <see cref="Predicate" />
    ///         subclasses of
    ///         <see cref="SemanticContext" />
    ///         within the scope of this outer class.
    ///     </p>
    /// </remarks>
    public abstract class SemanticContext
    {
        /// <summary>
        ///     The default
        ///     <see cref="SemanticContext" />
        ///     , which is semantically equivalent to
        ///     a predicate of the form
        ///     <c />
        ///     true}?}.
        /// </summary>
        public static readonly SemanticContext None = new Predicate();

        /// <summary>
        ///     For context independent predicates, we evaluate them without a local
        ///     context (i.e., null context).
        /// </summary>
        /// <remarks>
        ///     For context independent predicates, we evaluate them without a local
        ///     context (i.e., null context). That way, we can evaluate them without
        ///     having to create proper rule-specific context during prediction (as
        ///     opposed to the parser, which creates them naturally). In a practical
        ///     sense, this avoids a cast exception from RuleContext to myruleContext.
        ///     <p>
        ///         For context dependent predicates, we must pass in a local context so that
        ///         references such as $arg evaluate properly as _localctx.arg. We only
        ///         capture context dependent predicates in the context in which we begin
        ///         prediction, so we passed in the outer context here in case of context
        ///         dependent predicate evaluation.
        ///     </p>
        /// </remarks>
        public abstract bool Eval<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            where ATNInterpreter : ATNSimulator;

        /// <summary>Evaluate the precedence predicates for the context and reduce the result.</summary>
        /// <param name="parser">The parser instance.</param>
        /// <param name="parserCallStack" />
        /// <returns>
        ///     The simplified semantic context after precedence predicates are
        ///     evaluated, which will be one of the following values.
        ///     <ul>
        ///         <li>
        ///             <see cref="None" />
        ///             : if the predicate simplifies to
        ///             <see langword="true" />
        ///             after
        ///             precedence predicates are evaluated.
        ///         </li>
        ///         <li>
        ///             <see langword="null" />
        ///             : if the predicate simplifies to
        ///             <see langword="false" />
        ///             after
        ///             precedence predicates are evaluated.
        ///         </li>
        ///         <li>
        ///             <c>this</c>
        ///             : if the semantic context is not changed as a result of
        ///             precedence predicate evaluation.
        ///         </li>
        ///         <li>
        ///             A non-
        ///             <see langword="null" />
        ///             <see cref="SemanticContext" />
        ///             : the new simplified
        ///             semantic context after precedence predicates are evaluated.
        ///         </li>
        ///     </ul>
        /// </returns>
        public virtual SemanticContext EvalPrecedence<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            where ATNInterpreter : ATNSimulator
        {
            return this;
        }

        public static SemanticContext And(SemanticContext a, SemanticContext b)
        {
            if (a == null || a == None)
            {
                return b;
            }

            if (b == null || b == None)
            {
                return a;
            }

            AND result = new(a, b);
            if (result.opnds.Length == 1)
            {
                return result.opnds[0];
            }

            return result;
        }

        /// <seealso cref="ParserATNSimulator.GetPredsForAmbigAlts(BitSet, ATNConfigSet, int)" />
        public static SemanticContext Or(SemanticContext a, SemanticContext b)
        {
            if (a == null)
            {
                return b;
            }

            if (b == null)
            {
                return a;
            }

            if (a == None || b == None)
            {
                return None;
            }

            OR result = new(a, b);
            if (result.opnds.Length == 1)
            {
                return result.opnds[0];
            }

            return result;
        }

        private static IList<PrecedencePredicate> FilterPrecedencePredicates(HashSet<SemanticContext> collection)
        {
            if (!collection.OfType<PrecedencePredicate>().Any())
            {
                Collections.EmptyList<PrecedencePredicate>();
            }

            List<PrecedencePredicate> result = collection.OfType<PrecedencePredicate>().ToList();
            collection.ExceptWith(result);
            return result;
        }

        public class Predicate : SemanticContext
        {
            public readonly bool isCtxDependent;

            public readonly int predIndex;
            public readonly int ruleIndex;

            protected internal Predicate()
            {
                // e.g., $i ref in pred
                ruleIndex = -1;
                predIndex = -1;
                isCtxDependent = false;
            }

            public Predicate(int ruleIndex, int predIndex, bool isCtxDependent)
            {
                this.ruleIndex = ruleIndex;
                this.predIndex = predIndex;
                this.isCtxDependent = isCtxDependent;
            }

            public override bool Eval<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                RuleContext localctx = isCtxDependent ? parserCallStack : null;
                return parser.Sempred(localctx, ruleIndex, predIndex);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ruleIndex, predIndex, isCtxDependent);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Predicate))
                {
                    return false;
                }

                if (this == obj)
                {
                    return true;
                }

                Predicate p = (Predicate) obj;
                return ruleIndex == p.ruleIndex && predIndex == p.predIndex && isCtxDependent == p.isCtxDependent;
            }

            public override string ToString()
            {
                return "{" + ruleIndex + ":" + predIndex + "}?";
            }
        }

        public class PrecedencePredicate : SemanticContext, IComparable<PrecedencePredicate>
        {
            public readonly int precedence;

            protected internal PrecedencePredicate()
            {
                precedence = 0;
            }

            public PrecedencePredicate(int precedence)
            {
                this.precedence = precedence;
            }

            public virtual int CompareTo(PrecedencePredicate o)
            {
                return precedence - o.precedence;
            }

            public override bool Eval<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                return parser.Precpred(parserCallStack, precedence);
            }

            public override SemanticContext EvalPrecedence<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                if (parser.Precpred(parserCallStack, precedence))
                {
                    return None;
                }

                return null;
            }

            public override int GetHashCode()
            {
                int hashCode = 1;
                hashCode = 31 * hashCode + precedence;
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is PrecedencePredicate))
                {
                    return false;
                }

                if (this == obj)
                {
                    return true;
                }

                PrecedencePredicate other = (PrecedencePredicate) obj;
                return precedence == other.precedence;
            }

            public override string ToString()
            {
                // precedence >= _precedenceStack.peek()
                return "{" + precedence + ">=prec}?";
            }
        }

        /// <summary>
        ///     This is the base class for semantic context "operators", which operate on
        ///     a collection of semantic context "operands".
        /// </summary>
        /// <since>4.3</since>
        public abstract class Operator : SemanticContext
        {
            /// <summary>Gets the operands for the semantic context operator.</summary>
            /// <returns>
            ///     a collection of
            ///     <see cref="SemanticContext" />
            ///     operands for the
            ///     operator.
            /// </returns>
            /// <since>4.3</since>
            [NotNull]
            public abstract ICollection<SemanticContext> Operands { get; }
        }

        /// <summary>
        ///     A semantic context which is true whenever none of the contained contexts
        ///     is false.
        /// </summary>
        public class AND : Operator
        {
            [NotNull] public readonly SemanticContext[] opnds;

            public AND([NotNull] SemanticContext a, [NotNull] SemanticContext b)
            {
                var operands = new HashSet<SemanticContext>();
                if (a is AND)
                {
                    operands.UnionWith(((AND) a).opnds);
                }
                else
                {
                    operands.Add(a);
                }

                if (b is AND)
                {
                    operands.UnionWith(((AND) b).opnds);
                }
                else
                {
                    operands.Add(b);
                }

                IList<PrecedencePredicate> precedencePredicates = FilterPrecedencePredicates(operands);
                if (precedencePredicates.Count > 0)
                {
                    // interested in the transition with the lowest precedence
                    PrecedencePredicate reduced = precedencePredicates.Min();
                    operands.Add(reduced);
                }

                opnds = operands.ToArray();
            }

            public override ICollection<SemanticContext> Operands => Arrays.AsList(opnds);

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (!(obj is AND))
                {
                    return false;
                }

                AND other = (AND) obj;
                return Arrays.Equals(opnds, other.opnds);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(opnds, typeof(AND));
            }

            /// <summary>
            ///     <inheritDoc />
            ///     <p>
            ///         The evaluation of predicates by this context is short-circuiting, but
            ///         unordered.
            ///     </p>
            /// </summary>
            public override bool Eval<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                foreach (SemanticContext opnd in opnds)
                {
                    if (!opnd.Eval(parser, parserCallStack))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override SemanticContext EvalPrecedence<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                bool differs = false;
                IList<SemanticContext> operands = new List<SemanticContext>();
                foreach (SemanticContext context in opnds)
                {
                    SemanticContext evaluated = context.EvalPrecedence(parser, parserCallStack);
                    differs |= evaluated != context;
                    if (evaluated == null)
                    {
                        // The AND context is false if any element is false
                        return null;
                    }

                    if (evaluated != None)
                    {
                        // Reduce the result by skipping true elements
                        operands.Add(evaluated);
                    }
                }

                if (!differs)
                {
                    return this;
                }

                if (operands.Count == 0)
                {
                    // all elements were true, so the AND context is true
                    return None;
                }

                SemanticContext result = operands[0];
                for (int i = 1;
                    i < operands.Count;
                    i++)
                {
                    result = And(result, operands[i]);
                }

                return result;
            }

            public override string ToString()
            {
                return Utils.Join("&&", opnds);
            }
        }

        /// <summary>
        ///     A semantic context which is true whenever at least one of the contained
        ///     contexts is true.
        /// </summary>
        public class OR : Operator
        {
            [NotNull] public readonly SemanticContext[] opnds;

            public OR([NotNull] SemanticContext a, [NotNull] SemanticContext b)
            {
                var operands = new HashSet<SemanticContext>();
                if (a is OR)
                {
                    operands.UnionWith(((OR) a).opnds);
                }
                else
                {
                    operands.Add(a);
                }

                if (b is OR)
                {
                    operands.UnionWith(((OR) b).opnds);
                }
                else
                {
                    operands.Add(b);
                }

                IList<PrecedencePredicate> precedencePredicates = FilterPrecedencePredicates(operands);
                if (precedencePredicates.Count > 0)
                {
                    // interested in the transition with the highest precedence
                    PrecedencePredicate reduced = precedencePredicates.Max();
                    operands.Add(reduced);
                }

                opnds = operands.ToArray();
            }

            public override ICollection<SemanticContext> Operands => Arrays.AsList(opnds);

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (!(obj is OR))
                {
                    return false;
                }

                OR other = (OR) obj;
                return Arrays.Equals(opnds, other.opnds);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(opnds, typeof(OR));
            }

            /// <summary>
            ///     <inheritDoc />
            ///     <p>
            ///         The evaluation of predicates by this context is short-circuiting, but
            ///         unordered.
            ///     </p>
            /// </summary>
            public override bool Eval<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                foreach (SemanticContext opnd in opnds)
                {
                    if (opnd.Eval(parser, parserCallStack))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override SemanticContext EvalPrecedence<Symbol, ATNInterpreter>(Recognizer<Symbol, ATNInterpreter> parser, RuleContext parserCallStack)
            {
                bool differs = false;
                IList<SemanticContext> operands = new List<SemanticContext>();
                foreach (SemanticContext context in opnds)
                {
                    SemanticContext evaluated = context.EvalPrecedence(parser, parserCallStack);
                    differs |= evaluated != context;
                    if (evaluated == None)
                    {
                        // The OR context is true if any element is true
                        return None;
                    }

                    if (evaluated != null)
                    {
                        // Reduce the result by skipping false elements
                        operands.Add(evaluated);
                    }
                }

                if (!differs)
                {
                    return this;
                }

                if (operands.Count == 0)
                {
                    // all elements were false, so the OR context is false
                    return null;
                }

                SemanticContext result = operands[0];
                for (int i = 1;
                    i < operands.Count;
                    i++)
                {
                    result = Or(result, operands[i]);
                }

                return result;
            }

            public override string ToString()
            {
                return Utils.Join("||", opnds);
            }
        }
    }
}