// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Parse;
using Antlr4.Runtime;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Semantics
{
    /**
     * Trigger checks for various kinds of attribute expressions.
     * no side-effects.
     */
    public class AttributeChecks : ActionSplitterListener
    {
        public IToken actionToken; // token within action
        public Alternative alt; // null if action outside of alt; could be in rule
        public ErrorManager errMgr;
        public Grammar g;
        public ActionAST node;
        public Rule r; // null if action outside of rule

        public AttributeChecks(Grammar g, Rule r, Alternative alt, ActionAST node, IToken actionToken)
        {
            this.g = g;
            this.r = r;
            this.alt = alt;
            this.node = node;
            this.actionToken = actionToken;
            errMgr = g.tool.errMgr;
        }

        // LISTENER METHODS

        // $x.y
        public virtual void QualifiedAttr(string expr, IToken x, IToken y)
        {
            if (g.IsLexer())
            {
                errMgr.GrammarError(ErrorType.ATTRIBUTE_IN_LEXER_ACTION,
                    g.fileName, x, x.Text + "." + y.Text, expr);
                return;
            }

            if (node.resolver.ResolveToAttribute(x.Text, node) != null)
            {
                // must be a member access to a predefined attribute like $ctx.foo
                Attr(expr, x);
                return;
            }

            if (node.resolver.ResolveToAttribute(x.Text, y.Text, node) == null)
            {
                Rule rref = IsolatedRuleRef(x.Text);
                if (rref != null)
                {
                    if (rref.args != null && rref.args.Get(y.Text) != null)
                    {
                        g.tool.errMgr.GrammarError(ErrorType.INVALID_RULE_PARAMETER_REF,
                            g.fileName, y, y.Text, rref.name, expr);
                    }
                    else
                    {
                        errMgr.GrammarError(ErrorType.UNKNOWN_RULE_ATTRIBUTE,
                            g.fileName, y, y.Text, rref.name, expr);
                    }
                }
                else if (!node.resolver.ResolvesToAttributeDict(x.Text, node))
                {
                    errMgr.GrammarError(ErrorType.UNKNOWN_SIMPLE_ATTRIBUTE,
                        g.fileName, x, x.Text, expr);
                }
                else
                {
                    errMgr.GrammarError(ErrorType.UNKNOWN_ATTRIBUTE_IN_SCOPE,
                        g.fileName, y, y.Text, expr);
                }
            }
        }

        public virtual void SetAttr(string expr, IToken x, IToken rhs)
        {
            if (g.IsLexer())
            {
                errMgr.GrammarError(ErrorType.ATTRIBUTE_IN_LEXER_ACTION,
                    g.fileName, x, x.Text, expr);
                return;
            }

            if (node.resolver.ResolveToAttribute(x.Text, node) == null)
            {
                ErrorType errorType = ErrorType.UNKNOWN_SIMPLE_ATTRIBUTE;
                if (node.resolver.ResolvesToListLabel(x.Text, node))
                {
                    // $ids for ids+=ID etc...
                    errorType = ErrorType.ASSIGNMENT_TO_LIST_LABEL;
                }

                errMgr.GrammarError(errorType,
                    g.fileName, x, x.Text, expr);
            }

            new AttributeChecks(g, r, alt, node, rhs).ExamineAction();
        }

        public virtual void Attr(string expr, IToken x)
        {
            if (g.IsLexer())
            {
                errMgr.GrammarError(ErrorType.ATTRIBUTE_IN_LEXER_ACTION,
                    g.fileName, x, x.Text, expr);
                return;
            }

            if (node.resolver.ResolveToAttribute(x.Text, node) == null)
            {
                if (node.resolver.ResolvesToToken(x.Text, node))
                {
                    return; // $ID for token ref or label of token
                }

                if (node.resolver.ResolvesToListLabel(x.Text, node))
                {
                    return; // $ids for ids+=ID etc...
                }

                if (IsolatedRuleRef(x.Text) != null)
                {
                    errMgr.GrammarError(ErrorType.ISOLATED_RULE_REF,
                        g.fileName, x, x.Text, expr);
                    return;
                }

                errMgr.GrammarError(ErrorType.UNKNOWN_SIMPLE_ATTRIBUTE,
                    g.fileName, x, x.Text, expr);
            }
        }

        public virtual void NonLocalAttr(string expr, IToken x, IToken y)
        {
            Rule r = g.GetRule(x.Text);
            if (r == null)
            {
                errMgr.GrammarError(ErrorType.UNDEFINED_RULE_IN_NONLOCAL_REF,
                    g.fileName, x, x.Text, y.Text, expr);
            }
            else if (r.ResolveToAttribute(y.Text, null) == null)
            {
                errMgr.GrammarError(ErrorType.UNKNOWN_RULE_ATTRIBUTE,
                    g.fileName, y, y.Text, x.Text, expr);
            }
        }

        public virtual void SetNonLocalAttr(string expr, IToken x, IToken y, IToken rhs)
        {
            Rule r = g.GetRule(x.Text);
            if (r == null)
            {
                errMgr.GrammarError(ErrorType.UNDEFINED_RULE_IN_NONLOCAL_REF,
                    g.fileName, x, x.Text, y.Text, expr);
            }
            else if (r.ResolveToAttribute(y.Text, null) == null)
            {
                errMgr.GrammarError(ErrorType.UNKNOWN_RULE_ATTRIBUTE,
                    g.fileName, y, y.Text, x.Text, expr);
            }
        }

        public virtual void Text(string text)
        {
        }

        public static void CheckAllAttributeExpressions(Grammar g)
        {
            foreach (ActionAST act in g.namedActions.Values)
            {
                AttributeChecks checker = new(g, null, null, act, act.Token);
                checker.ExamineAction();
            }

            foreach (Rule r in g.rules.Values)
            {
                foreach (ActionAST a in r.namedActions.Values)
                {
                    AttributeChecks checker = new(g, r, null, a, a.Token);
                    checker.ExamineAction();
                }

                for (int i = 1;
                    i <= r.numberOfAlts;
                    i++)
                {
                    Alternative alt = r.alt[i];
                    foreach (ActionAST a in alt.actions)
                    {
                        AttributeChecks checker =
                            new(g, r, alt, a, a.Token);
                        checker.ExamineAction();
                    }
                }

                foreach (GrammarAST e in r.exceptions)
                {
                    ActionAST a = (ActionAST) e.GetChild(1);
                    AttributeChecks checker = new(g, r, null, a, a.Token);
                    checker.ExamineAction();
                }

                if (r.finallyAction != null)
                {
                    AttributeChecks checker =
                        new(g, r, null, r.finallyAction, r.finallyAction.Token);
                    checker.ExamineAction();
                }
            }
        }

        public virtual void ExamineAction()
        {
            //System.out.println("examine "+actionToken);
            ANTLRStringStream @in = new(actionToken.Text);
            @in.Line = actionToken.Line;
            @in.CharPositionInLine = actionToken.CharPositionInLine;
            ActionSplitter splitter = new ActionSplitter(@in, this);
            // forces eval, triggers listener methods
            node.chunks = splitter.GetActionTokens();
        }

        // don't care
        public virtual void TemplateInstance(string expr)
        {
        }

        public virtual void IndirectTemplateInstance(string expr)
        {
        }

        public virtual void SetExprAttribute(string expr)
        {
        }

        public virtual void SetSTAttribute(string expr)
        {
        }

        public virtual void TemplateExpr(string expr)
        {
        }

        // SUPPORT

        public virtual Rule IsolatedRuleRef(string x)
        {
            if (node.resolver is Grammar)
            {
                return null;
            }

            if (x.Equals(r.name))
            {
                return r;
            }

            IList<LabelElementPair> labels = null;
            if (node.resolver is Rule)
            {
                r.GetElementLabelDefs().TryGetValue(x, out labels);
            }
            else if (node.resolver is Alternative)
            {
                ((Alternative) node.resolver).labelDefs.TryGetValue(x, out labels);
            }

            if (labels != null)
            {
                // it's a label ref. is it a rule label?
                LabelElementPair anyLabelDef = labels[0];
                if (anyLabelDef.type == LabelType.RULE_LABEL)
                {
                    return g.GetRule(anyLabelDef.element.Text);
                }
            }

            if (node.resolver is Alternative)
            {
                IList<GrammarAST> value;
                if (((Alternative) node.resolver).ruleRefs.TryGetValue(x, out value) && value != null)
                {
                    return g.GetRule(x);
                }
            }

            return null;
        }
    }
}