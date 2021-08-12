// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Misc;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Utility;
using Antlr4.Tool.Ast;

namespace Antlr4.Tool
{
    public class Rule : AttributeResolver
    {
        /**
         * Rule refs have a predefined set of attributes as well as
         * the return values and args.
         * 
         * These must be consistent with ActionTranslator.rulePropToModelMap, ...
         */
        public static readonly AttributeDict predefinedRulePropertiesDict =
            new(AttributeDict.DictType.PREDEFINED_RULE);

        public static readonly ISet<string> validLexerCommands =
            new HashSet<string>
            {
                // CALLS
                "mode",
                "pushMode",
                "type",
                "channel",

                // ACTIONS
                "popMode",
                "skip",
                "more"
            };

        public int actionIndex = -1; // if lexer; 0..n-1 for n actions in a rule

        /**
         * Track all executable actions other than named actions like @init
         * and catch/finally (not in an alt). Also tracks predicates, rewrite actions.
         * We need to examine these actions before code generation so
         * that we can detect refs to $rule.attr etc...
         * 
         * This tracks per rule; Alternative objs also track per alt.
         */
        public IList<ActionAST> actions = new List<ActionAST>();

        /**
         * 1..n alts
         */
        public Alternative[] alt;

        public AttributeDict args;

        public RuleAST ast;
        private string baseContext;

        /**
         * Track exception handlers; points at "catch" node of (catch exception action)
         * don't track finally action
         */
        public IList<GrammarAST> exceptions = new List<GrammarAST>();

        public ActionAST finallyAction;

        /**
         * In which grammar does this rule live?
         */
        public Grammar g;

        /**
         * All rules have unique index 0..n-1
         */
        public int index;

        public bool isStartRule = true; // nobody calls us
        public AttributeDict locals;

        /**
         * If we're in a lexer grammar, we might be in a mode
         */
        public string mode;

        public IList<GrammarAST> modifiers;

        public string name;

        /**
         * Map a name to an action for this rule like @init {...}.
         * The code generator will use this to fill holes in the rule template.
         * I track the AST node for the action in case I need the line number
         * for errors.
         */
        public IDictionary<string, ActionAST> namedActions =
            new Dictionary<string, ActionAST>();

        public int numberOfAlts;
        public AttributeDict retvals;

        static Rule()
        {
            predefinedRulePropertiesDict.Add(new AttributeNode("parser"));
            predefinedRulePropertiesDict.Add(new AttributeNode("text"));
            predefinedRulePropertiesDict.Add(new AttributeNode("start"));
            predefinedRulePropertiesDict.Add(new AttributeNode("stop"));
            predefinedRulePropertiesDict.Add(new AttributeNode("ctx"));
        }

        public Rule(Grammar g, string name, RuleAST ast, int numberOfAlts)
        {
            this.g = g;
            this.name = name;
            this.ast = ast;
            this.numberOfAlts = numberOfAlts;
            alt = new Alternative[numberOfAlts + 1]; // 1..n
            for (int i = 1;
                i <= numberOfAlts;
                i++)
            {
                alt[i] = new Alternative(this, i);
            }
        }

        /**
         * $x		Attribute: rule arguments, return values, predefined rule prop.
         */
        public virtual AttributeNode ResolveToAttribute(string x, ActionAST node)
        {
            if (args != null)
            {
                AttributeNode a = args.Get(x);
                if (a != null)
                {
                    return a;
                }
            }

            if (retvals != null)
            {
                AttributeNode a = retvals.Get(x);
                if (a != null)
                {
                    return a;
                }
            }

            if (locals != null)
            {
                AttributeNode a = locals.Get(x);
                if (a != null)
                {
                    return a;
                }
            }

            AttributeDict properties = GetPredefinedScope(LabelType.RULE_LABEL);
            return properties.Get(x);
        }

        /**
         * $x.y	Attribute: x is surrounding rule, label ref (in any alts)
         */
        public virtual AttributeNode ResolveToAttribute(string x, string y, ActionAST node)
        {
            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            if (anyLabelDef != null)
            {
                if (anyLabelDef.type == LabelType.RULE_LABEL)
                {
                    return g.GetRule(anyLabelDef.element.Text).ResolveRetvalOrProperty(y);
                }

                AttributeDict scope = GetPredefinedScope(anyLabelDef.type);
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
                   (anyLabelDef.type == LabelType.RULE_LABEL ||
                    anyLabelDef.type == LabelType.TOKEN_LABEL);
        }

        public virtual bool ResolvesToListLabel(string x, ActionAST node)
        {
            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            return anyLabelDef != null &&
                   (anyLabelDef.type == LabelType.RULE_LIST_LABEL ||
                    anyLabelDef.type == LabelType.TOKEN_LIST_LABEL);
        }

        public virtual bool ResolvesToToken(string x, ActionAST node)
        {
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

            return false;
        }

        public virtual string GetBaseContext()
        {
            if (!String.IsNullOrEmpty(baseContext))
            {
                return baseContext;
            }

            string optionBaseContext = ast.GetOptionString("baseContext");
            if (!String.IsNullOrEmpty(optionBaseContext))
            {
                return optionBaseContext;
            }

            int variantDelimiter = name.IndexOf(ATNSimulator.RuleVariantDelimiter);
            if (variantDelimiter >= 0)
            {
                return name.Substring(0, variantDelimiter);
            }

            return name;
        }

        public virtual void SetBaseContext(string baseContext)
        {
            this.baseContext = baseContext;
        }

        public virtual void DefineActionInAlt(int currentAlt, ActionAST actionAST)
        {
            actions.Add(actionAST);
            alt[currentAlt].actions.Add(actionAST);
            if (g.IsLexer())
            {
                DefineLexerAction(actionAST);
            }
        }

        /**
         * Lexer actions are numbered across rules 0..n-1
         */
        public virtual void DefineLexerAction(ActionAST actionAST)
        {
            actionIndex = g.lexerActions.Count;
            if (!g.lexerActions.ContainsKey(actionAST))
            {
                g.lexerActions[actionAST] = actionIndex;
            }
        }

        public virtual void DefinePredicateInAlt(int currentAlt, PredAST predAST)
        {
            actions.Add(predAST);
            alt[currentAlt].actions.Add(predAST);
            if (!g.sempreds.ContainsKey(predAST))
            {
                g.sempreds[predAST] = g.sempreds.Count;
            }
        }

        public virtual AttributeNode ResolveRetvalOrProperty(string y)
        {
            if (retvals != null)
            {
                AttributeNode a = retvals.Get(y);
                if (a != null)
                {
                    return a;
                }
            }

            AttributeDict d = GetPredefinedScope(LabelType.RULE_LABEL);
            return d.Get(y);
        }

        public virtual ISet<string> GetTokenRefs()
        {
            ISet<string> refs = new HashSet<string>();
            for (int i = 1;
                i <= numberOfAlts;
                i++)
            {
                refs.UnionWith(alt[i].tokenRefs.Keys);
            }

            return refs;
        }

        public virtual ISet<string> GetElementLabelNames()
        {
            ISet<string> refs = new HashSet<string>();
            for (int i = 1;
                i <= numberOfAlts;
                i++)
            {
                refs.UnionWith(alt[i].labelDefs.Keys);
            }

            if (refs.Count == 0)
            {
                return null;
            }

            return refs;
        }

        public virtual MultiMap<string, LabelElementPair> GetElementLabelDefs()
        {
            var defs =
                new MultiMap<string, LabelElementPair>();
            for (int i = 1;
                i <= numberOfAlts;
                i++)
            {
                foreach (IList<LabelElementPair> pairs in alt[i].labelDefs.Values)
                {
                    foreach (LabelElementPair p in pairs)
                    {
                        defs.Map(p.label.Text, p);
                    }
                }
            }

            return defs;
        }

        public virtual bool HasAltSpecificContexts()
        {
            return GetAltLabels() != null;
        }

        /**
         * Used for recursive rules (subclass), which have 1 alt, but many original alts
         */
        public virtual int GetOriginalNumberOfAlts()
        {
            return numberOfAlts;
        }

        /**
         * Get {@code #} labels. The keys of the map are the labels applied to outer
         * alternatives of a lexer rule, and the values are collections of pairs
         * (alternative number and {@link AltAST}) identifying the alternatives with
         * this label. Unlabeled alternatives are not included in the result.
         */
        public virtual IDictionary<string, IList<Tuple<int, AltAST>>> GetAltLabels()
        {
            IDictionary<string, IList<Tuple<int, AltAST>>> labels = new LinkedHashMap<string, IList<Tuple<int, AltAST>>>();
            for (int i = 1;
                i <= numberOfAlts;
                i++)
            {
                GrammarAST altLabel = alt[i].ast.altLabel;
                if (altLabel != null)
                {
                    IList<Tuple<int, AltAST>> list;
                    if (!labels.TryGetValue(altLabel.Text, out list) || list == null)
                    {
                        list = new List<Tuple<int, AltAST>>();
                        labels[altLabel.Text] = list;
                    }

                    list.Add(Tuple.Create(i, alt[i].ast));
                }
            }

            if (labels.Count == 0)
            {
                return null;
            }

            return labels;
        }

        public virtual IList<AltAST> GetUnlabeledAltASTs()
        {
            IList<AltAST> alts = new List<AltAST>();
            for (int i = 1;
                i <= numberOfAlts;
                i++)
            {
                GrammarAST altLabel = alt[i].ast.altLabel;
                if (altLabel == null)
                {
                    alts.Add(alt[i].ast);
                }
            }

            if (alts.Count == 0)
            {
                return null;
            }

            return alts;
        }

        public virtual Rule resolveToRule(string x)
        {
            if (x.Equals(name))
            {
                return this;
            }

            LabelElementPair anyLabelDef = GetAnyLabelDef(x);
            if (anyLabelDef != null && anyLabelDef.type == LabelType.RULE_LABEL)
            {
                return g.GetRule(anyLabelDef.element.Text);
            }

            return g.GetRule(x);
        }

        public virtual LabelElementPair GetAnyLabelDef(string x)
        {
            IList<LabelElementPair> labels;
            if (GetElementLabelDefs().TryGetValue(x, out labels) && labels != null)
            {
                return labels[0];
            }

            return null;
        }

        public virtual AttributeDict GetPredefinedScope(LabelType ltype)
        {
            string grammarLabelKey = g.GetTypeString() + ":" + ltype;

            AttributeDict result;
            Grammar.grammarAndLabelRefTypeToScope.TryGetValue(grammarLabelKey, out result);
            return result;
        }

        public virtual bool IsFragment()
        {
            if (modifiers == null)
            {
                return false;
            }

            foreach (GrammarAST a in modifiers)
            {
                if (a.Text.Equals("fragment"))
                {
                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (!(obj is Rule))
            {
                return false;
            }

            return name.Equals(((Rule) obj).name);
        }

        public override string ToString()
        {
            StringBuilder buf = new();
            buf.Append("Rule{name=").Append(name);
            if (args != null)
            {
                buf.Append(", args=").Append(args);
            }

            if (retvals != null)
            {
                buf.Append(", retvals=").Append(retvals);
            }

            buf.Append("}");
            return buf.ToString();
        }
    }
}