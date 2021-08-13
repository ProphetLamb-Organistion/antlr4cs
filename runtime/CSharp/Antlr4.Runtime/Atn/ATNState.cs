// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime.Atn
{
    /// <summary>
    ///     The following images show the relation of states and
    ///     <see cref="transitions" />
    ///     for various grammar constructs.
    ///     <ul>
    ///         <li>
    ///             Solid edges marked with an &#0949; indicate a required
    ///             <see cref="EpsilonTransition" />
    ///             .
    ///         </li>
    ///         <li>
    ///             Dashed edges indicate locations where any transition derived from
    ///             <see cref="Transition" />
    ///             might appear.
    ///         </li>
    ///         <li>
    ///             Dashed nodes are place holders for either a sequence of linked
    ///             <see cref="BasicState" />
    ///             states or the inclusion of a block representing a nested
    ///             construct in one of the forms below.
    ///         </li>
    ///         <li>
    ///             Nodes showing multiple outgoing alternatives with a
    ///             <c>...</c>
    ///             support
    ///             any number of alternatives (one or more). Nodes without the
    ///             <c>...</c>
    ///             only
    ///             support the exact number of alternatives shown in the diagram.
    ///         </li>
    ///     </ul>
    ///     <h2>Basic Blocks</h2>
    ///     <h3>Rule</h3>
    ///     <embed src="images/Rule.svg" type="image/svg+xml" />
    ///     <h3>Block of 1 or more alternatives</h3>
    ///     <embed src="images/Block.svg" type="image/svg+xml" />
    ///     <h2>Greedy Loops</h2>
    ///     <h3>
    ///         Greedy Closure:
    ///         <c>(...)*</c>
    ///     </h3>
    ///     <embed src="images/ClosureGreedy.svg" type="image/svg+xml" />
    ///     <h3>
    ///         Greedy Positive Closure:
    ///         <c>(...)+</c>
    ///     </h3>
    ///     <embed src="images/PositiveClosureGreedy.svg" type="image/svg+xml" />
    ///     <h3>
    ///         Greedy Optional:
    ///         <c>(...)?</c>
    ///     </h3>
    ///     <embed src="images/OptionalGreedy.svg" type="image/svg+xml" />
    ///     <h2>Non-Greedy Loops</h2>
    ///     <h3>
    ///         Non-Greedy Closure:
    ///         <c>(...)*?</c>
    ///     </h3>
    ///     <embed src="images/ClosureNonGreedy.svg" type="image/svg+xml" />
    ///     <h3>
    ///         Non-Greedy Positive Closure:
    ///         <c>(...)+?</c>
    ///     </h3>
    ///     <embed src="images/PositiveClosureNonGreedy.svg" type="image/svg+xml" />
    ///     <h3>
    ///         Non-Greedy Optional:
    ///         <c>(...)??</c>
    ///     </h3>
    ///     <embed src="images/OptionalNonGreedy.svg" type="image/svg+xml" />
    /// </summary>
    public abstract class ATNState
    {
        public const int InitialNumTransitions = 4;

        public const int InvalidStateNumber = -1;

        public static readonly ReadOnlyCollection<string> serializationNames = new(Arrays.AsList("INVALID", "BASIC", "RULE_START", "BLOCK_START", "PLUS_BLOCK_START",
            "STAR_BLOCK_START", "TOKEN_START", "RULE_STOP", "BLOCK_END", "STAR_LOOP_BACK", "STAR_LOOP_ENTRY", "PLUS_LOOP_BACK", "LOOP_END"));

        /// <summary>Track the transitions emanating from this ATN state.</summary>
        protected internal readonly List<Transition> transitions = new(InitialNumTransitions);

        /// <summary>Which ATN are we in?</summary>
        public ATN atn = null;

        public bool epsilonOnlyTransitions;

        /// <summary>Used to cache lookahead during parsing, not used during construction</summary>
        public IntervalSet nextTokenWithinRule;

        protected internal List<Transition> optimizedTransitions;

        public int ruleIndex;

        public int stateNumber = InvalidStateNumber;

        public ATNState()
        {
            optimizedTransitions = transitions;
        }

        /// <summary>Gets the state number.</summary>
        /// <returns>the state number</returns>
        public int StateNumber =>
            // at runtime, we don't have Rule objects
            stateNumber;

        /// <summary>
        ///     For all states except
        ///     <see cref="RuleStopState" />
        ///     , this returns the state
        ///     number. Returns -1 for stop states.
        /// </summary>
        /// <returns>
        ///     -1 for
        ///     <see cref="RuleStopState" />
        ///     , otherwise the state number
        /// </returns>
        public virtual int NonStopStateNumber => StateNumber;

        public virtual bool IsNonGreedyExitState => false;

        public virtual Transition[] Transitions => transitions.ToArray();

        public virtual int NumberOfTransitions => transitions.Count;

        public abstract StateType StateType { get; }

        public bool OnlyHasEpsilonTransitions => epsilonOnlyTransitions;

        public virtual bool IsOptimized => optimizedTransitions != transitions;

        public virtual int NumberOfOptimizedTransitions => optimizedTransitions.Count;

        public override int GetHashCode()
        {
            return stateNumber;
        }

        public override bool Equals(object o)
        {
            // are these states same object?
            if (o is ATNState)
            {
                return stateNumber == ((ATNState) o).stateNumber;
            }

            return false;
        }

        public override string ToString()
        {
            return stateNumber.ToString();
        }

        public virtual void AddTransition(Transition e)
        {
            AddTransition(transitions.Count, e);
        }

        public virtual void AddTransition(int index, Transition e)
        {
            if (transitions.Count == 0)
            {
                epsilonOnlyTransitions = e.IsEpsilon;
            }
            else
            {
                if (epsilonOnlyTransitions != e.IsEpsilon)
                {
                    Console.Error.WriteLine("ATN state {0} has both epsilon and non-epsilon transitions.", stateNumber);
                    epsilonOnlyTransitions = false;
                }
            }

            transitions.Insert(index, e);
        }

        public virtual Transition Transition(int i)
        {
            return transitions[i];
        }

        public virtual void SetTransition(int i, Transition e)
        {
            transitions[i] = e;
        }

        public virtual void RemoveTransition(int index)
        {
            transitions.RemoveAt(index);
        }

        public virtual void SetRuleIndex(int ruleIndex)
        {
            this.ruleIndex = ruleIndex;
        }

        public virtual Transition GetOptimizedTransition(int i)
        {
            return optimizedTransitions[i];
        }

        public virtual void AddOptimizedTransition(Transition e)
        {
            if (!IsOptimized)
            {
                optimizedTransitions = new List<Transition>();
            }

            optimizedTransitions.Add(e);
        }

        public virtual void SetOptimizedTransition(int i, Transition e)
        {
            if (!IsOptimized)
            {
                throw new InvalidOperationException();
            }

            optimizedTransitions[i] = e;
        }

        public virtual void RemoveOptimizedTransition(int i)
        {
            if (!IsOptimized)
            {
                throw new InvalidOperationException();
            }

            optimizedTransitions.RemoveAt(i);
        }
    }
}