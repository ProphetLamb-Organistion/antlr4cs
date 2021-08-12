// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Utility;

namespace Antlr4.Runtime
{
    /// <summary>A rule context is a record of a single rule invocation.</summary>
    /// <remarks>
    ///     A rule context is a record of a single rule invocation.
    ///     We form a stack of these context objects using the parent
    ///     pointer. A parent pointer of null indicates that the current
    ///     context is the bottom of the stack. The ParserRuleContext subclass
    ///     as a children list so that we can turn this data structure into a
    ///     tree.
    ///     The root node always has a null pointer and invokingState of -1.
    ///     Upon entry to parsing, the first invoked rule function creates a
    ///     context object (a subclass specialized for that rule such as
    ///     SContext) and makes it the root of a parse tree, recorded by field
    ///     Parser._ctx.
    ///     public final SContext s() throws RecognitionException {
    ///     SContext _localctx = new SContext(_ctx, getState()); &lt;-- create new node
    ///     enterRule(_localctx, 0, RULE_s);                     &lt;-- push it
    ///     ...
    ///     exitRule();                                          &lt;-- pop back to _localctx
    ///     return _localctx;
    ///     }
    ///     A subsequent rule invocation of r from the start rule s pushes a
    ///     new context object for r whose parent points at s and use invoking
    ///     state is the state with r emanating as edge label.
    ///     The invokingState fields from a context object to the root
    ///     together form a stack of rule indication states where the root
    ///     (bottom of the stack) has a -1 sentinel value. If we invoke start
    ///     symbol s then call r1, which calls r2, the  would look like
    ///     this:
    ///     SContext[-1]   &lt;- root node (bottom of the stack)
    ///     R1Context[p]   &lt;- p in rule s called r1
    ///     R2Context[q]   &lt;- q in rule r1 called r2
    ///     So the top of the stack, _ctx, represents a call to the current
    ///     rule and it holds the return address from another rule that invoke
    ///     to this rule. To invoke a rule, we must always have a current context.
    ///     The parent contexts are useful for computing lookahead sets and
    ///     getting error information.
    ///     These objects are used during parsing and prediction.
    ///     For the special case of parsers, we use the subclass
    ///     ParserRuleContext.
    /// </remarks>
    /// <seealso cref="ParserRuleContext" />
    public class RuleContext : IRuleNode
    {
        /// <summary>
        ///     What state invoked the rule associated with this context?
        ///     The "return address" is the followState of invokingState
        ///     If parent is null, this should be -1 this context object represents
        ///     the start rule.
        /// </summary>
        public int invokingState = -1;

        /// <summary>What context invoked this rule?</summary>
        public RuleContext parent;

        public RuleContext()
        {
        }

        public RuleContext(RuleContext parent, int invokingState)
        {
            this.parent = parent;
            //if ( parent!=null ) System.out.println("invoke "+stateNumber+" from "+parent);
            this.invokingState = invokingState;
        }

        /// <summary>
        ///     A context is empty if there is no invoking state; meaning nobody called
        ///     current context.
        /// </summary>
        public virtual bool IsEmpty => invokingState == -1;

        public virtual RuleContext Parent => parent;

        public virtual RuleContext Payload => this;

        public virtual int RuleIndex => -1;

        /// <summary>
        ///     For rule associated with this parse tree internal node, return
        ///     the outer alternative number used to match the input.
        /// </summary>
        /// <remarks>
        ///     For rule associated with this parse tree internal node, return
        ///     the outer alternative number used to match the input. Default
        ///     implementation does not compute nor store this alt num. Create
        ///     a subclass of ParserRuleContext with backing field and set
        ///     option contextSuperClass.
        ///     to set it.
        /// </remarks>
        /// <since>4.5.3</since>
        /// <summary>Set the outer alternative number for this context node.</summary>
        /// <remarks>
        ///     Set the outer alternative number for this context node. Default
        ///     implementation does nothing to avoid backing field overhead for
        ///     trees that don't need it.  Create
        ///     a subclass of ParserRuleContext with backing field and set
        ///     option contextSuperClass.
        /// </remarks>
        /// <since>4.5.3</since>
        public virtual int OuterAlternative
        {
            get => ATN.InvalidAltNumber;
            set
            {
                int altNumber = value;
            }
        }

        public virtual Interval SourceInterval =>
            // satisfy the ParseTree / SyntaxTree interface
            Interval.Invalid;

        RuleContext IRuleNode.RuleContext => this;

        IRuleNode IRuleNode.Parent => Parent;

        IParseTree IParseTree.Parent => Parent;

        ITree ITree.Parent => Parent;

        object ITree.Payload => Payload;

        /// <summary>Return the combined text of all child nodes.</summary>
        /// <remarks>
        ///     Return the combined text of all child nodes. This method only considers
        ///     tokens which have been added to the parse tree.
        ///     <p />
        ///     Since tokens on hidden channels (e.g. whitespace or comments) are not
        ///     added to the parse trees, they will not appear in the output of this
        ///     method.
        /// </remarks>
        public virtual string GetText()
        {
            if (ChildCount == 0)
            {
                return String.Empty;
            }

            StringBuilder builder = new();
            for (int i = 0;
                i < ChildCount;
                i++)
            {
                builder.Append(GetChild(i).GetText());
            }

            return builder.ToString();
        }

        public virtual IParseTree GetChild(int i)
        {
            return null;
        }

        ITree ITree.GetChild(int i)
        {
            return GetChild(i);
        }

        public virtual int ChildCount => 0;

        public virtual T Accept<T>(IParseTreeVisitor<T> visitor)
        {
            return visitor.VisitChildren(this);
        }

        /// <summary>
        ///     Print out a whole tree, not just a node, in LISP format
        ///     (root child1 ..
        /// </summary>
        /// <remarks>
        ///     Print out a whole tree, not just a node, in LISP format
        ///     (root child1 .. childN). Print just a node if this is a leaf.
        ///     We have to know the recognizer so we can get rule names.
        /// </remarks>
        public virtual string ToStringTree([AllowNull] Parser recog)
        {
            return Trees.ToStringTree(this, recog);
        }

        public virtual string ToStringTree()
        {
            return ToStringTree((IList<string>) null);
        }

        public static RuleContext GetChildContext(RuleContext parent, int invokingState)
        {
            return new RuleContext(parent, invokingState);
        }

        public virtual int Depth()
        {
            int n = 0;
            RuleContext p = this;
            while (p != null)
            {
                p = p.parent;
                n++;
            }

            return n;
        }

        /// <summary>
        ///     Print out a whole tree, not just a node, in LISP format
        ///     (root child1 ..
        /// </summary>
        /// <remarks>
        ///     Print out a whole tree, not just a node, in LISP format
        ///     (root child1 .. childN). Print just a node if this is a leaf.
        /// </remarks>
        public virtual string ToStringTree([AllowNull] IList<string> ruleNames)
        {
            return Trees.ToStringTree(this, ruleNames);
        }

        public override string ToString()
        {
            return ToString((IList<string>) null, null);
        }

        public string ToString(IRecognizer recog)
        {
            return ToString(recog, ParserRuleContext.EmptyContext);
        }

        public string ToString([AllowNull] IList<string> ruleNames)
        {
            return ToString(ruleNames, null);
        }

        // recog null unless ParserRuleContext, in which case we use subclass toString(...)
        public virtual string ToString(IRecognizer recog, [AllowNull] RuleContext stop)
        {
            string[] ruleNames = recog != null ? recog.RuleNames : null;
            IList<string> ruleNamesList = ruleNames != null ? Arrays.AsList(ruleNames) : null;
            return ToString(ruleNamesList, stop);
        }

        public virtual string ToString([AllowNull] IList<string> ruleNames, [AllowNull] RuleContext stop)
        {
            StringBuilder buf = new();
            RuleContext p = this;
            buf.Append("[");
            while (p != null && p != stop)
            {
                if (ruleNames == null)
                {
                    if (!p.IsEmpty)
                    {
                        buf.Append(p.invokingState);
                    }
                }
                else
                {
                    int ruleIndex = p.RuleIndex;
                    string ruleName = ruleIndex >= 0 && ruleIndex < ruleNames.Count ? ruleNames[ruleIndex] : ruleIndex.ToString();
                    buf.Append(ruleName);
                }

                if (p.parent != null && (ruleNames != null || !p.parent.IsEmpty))
                {
                    buf.Append(" ");
                }

                p = p.parent;
            }

            buf.Append("]");
            return buf.ToString();
        }
    }
}