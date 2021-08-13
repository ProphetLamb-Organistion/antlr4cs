// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System.Collections.Generic;
using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Tool
{
    /**
     * An outermost alternative for a rule.  We don't track inner alternatives.
     */
    public class Alternative : AttributeResolver
    {
        // track all token, rule, label refs in rewrite (right of ->)
        //public List<GrammarAST> rewriteElements = new ArrayList<GrammarAST>();

        /**
         * Track all executable actions other than named actions like @init
         * and catch/finally (not in an alt). Also tracks predicates, rewrite actions.
         * We need to examine these actions before code generation so
         * that we can detect refs to $rule.attr etc...
         * 
         * This tracks per alt
         */
        public IList<ActionAST> actions = new List<ActionAST>();

        /**
         * What alternative number is this outermost alt? 1..n
         */
        public int altNum;

        public AltAST ast;

        /**
         * A list of all LabelElementPair attached to tokens like id=ID, ids+=ID
         */
        public MultiMap<string, LabelElementPair> labelDefs = new();

        public Rule rule;

        // all rule refs in this alt
        public MultiMap<string, GrammarAST> ruleRefs = new();

        // does not include labels
        public MultiMap<string, GrammarAST> ruleRefsInActions = new();

        // token IDs, string literals in this alt
        public MultiMap<string, TerminalAST> tokenRefs = new();

        // does not include labels
        public MultiMap<string, GrammarAST> tokenRefsInActions = new();

        public Alternative(Rule r, int altNum)
        {
            rule = r;
            this.altNum = altNum;
        }

        public virtual bool ResolvesToToken(string x, ActionAST node)
        {
            if (tokenRefs.ContainsKey(x) && tokenRefs[x] != null)
            {
                return true;
            }

            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            if (anyLabelDef != null && anyLabelDef.type == LabelType.TOKEN_LABEL)
            {
                return true;
            }

            return false;
        }

        public virtual bool ResolvesToAttributeDict(string x, ActionAST node)
        {
            if (ResolvesToToken(x, node))
            {
                return true;
            }

            if (ruleRefs.ContainsKey(x) && ruleRefs[x] != null)
            {
                return true; // rule ref in this alt?
            }

            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            if (anyLabelDef != null && anyLabelDef.type == LabelType.RULE_LABEL)
            {
                return true;
            }

            return false;
        }

        /**
         * $x		Attribute: rule arguments, return values, predefined rule prop.
         */
        public virtual AttributeNode ResolveToAttribute(string x, ActionAST node)
        {
            return rule.ResolveToAttribute(x, node); // reuse that code
        }

        /**
         * $x.y, x can be surrounding rule, token/rule/label ref. y is visible
         * attr in that dictionary.  Can't see args on rule refs.
         */
        public virtual AttributeNode ResolveToAttribute(string x, string y, ActionAST node)
        {
            if (tokenRefs.ContainsKey(x) && tokenRefs[x] != null)
            {
                // token ref in this alt?
                return rule.GetPredefinedScope(LabelType.TOKEN_LABEL).Get(y);
            }

            if (ruleRefs.ContainsKey(x) && ruleRefs[x] != null)
            {
                // rule ref in this alt?
                // look up rule, ask it to resolve y (must be retval or predefined)
                return rule.g.GetRule(x).ResolveRetvalOrProperty(y);
            }

            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            if (anyLabelDef != null && anyLabelDef.type == LabelType.RULE_LABEL)
            {
                return rule.g.GetRule(anyLabelDef.element.Text).ResolveRetvalOrProperty(y);
            }

            if (anyLabelDef != null)
            {
                AttributeDict scope = rule.GetPredefinedScope(anyLabelDef.type);
                if (scope == null)
                {
                    return null;
                }

                return scope.Get(y);
            }

            return null;
        }

        public virtual bool ResolvesToLabel(string x, ActionAST node)
        {
            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            return anyLabelDef != null &&
                   (anyLabelDef.type == LabelType.TOKEN_LABEL ||
                    anyLabelDef.type == LabelType.RULE_LABEL);
        }

        public virtual bool ResolvesToListLabel(string x, ActionAST node)
        {
            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            return anyLabelDef != null &&
                   (anyLabelDef.type == LabelType.RULE_LIST_LABEL ||
                    anyLabelDef.type == LabelType.TOKEN_LIST_LABEL);
        }

        public virtual LabelElementPair GetAnyLabelDef(string x)
        {
            IList<LabelElementPair> labels;
            if (labelDefs.TryGetValue(x, out labels) && labels != null)
            {
                return labels[0];
            }

            return null;
        }

        /**
         * x can be ruleref or rule label.
         */
        public virtual Rule ResolveToRule(string x)
        {
            if (ruleRefs.ContainsKey(x) && ruleRefs[x] != null)
            {
                return rule.g.GetRule(x);
            }

            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            if (anyLabelDef != null && anyLabelDef.type == LabelType.RULE_LABEL)
            {
                return rule.g.GetRule(anyLabelDef.element.Text);
            }

            return null;
        }
    }
}