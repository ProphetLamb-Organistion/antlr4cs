// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if true
using Antlr4.Runtime.Misc;
#else
using System.Diagnostics.CodeAnalysis;
#endif

using Antlr4.Analysis;
using Antlr4.Automata;
using Antlr4.Runtime;
using Antlr4.Tool;
using Antlr4.Tool.Ast;

namespace Antlr4.Semantics
{
    /**
     * Do as much semantic checking as we can and fill in grammar
     * with rules, actions, and token definitions.
     * The only side effects are in the grammar passed to process().
     * We consume a bunch of memory here while we build up data structures
     * to perform checking, but all of it goes away after this pipeline object
     * gets garbage collected.
     * 
     * After this pipeline finishes, we can be sure that the grammar
     * is syntactically correct and that it's semantically correct enough for us
     * to attempt grammar analysis. We have assigned all token types.
     * Note that imported grammars bring in token and rule definitions
     * but only the root grammar and any implicitly created lexer grammar
     * get their token definitions filled up. We are treating the
     * imported grammars like includes.
     * 
     * The semantic pipeline works on root grammars (those that do the importing,
     * if any). Upon entry to the semantic pipeline, all imported grammars
     * should have been loaded into delegate grammar objects with their
     * ASTs created.  The pipeline does the BasicSemanticChecks on the
     * imported grammar before collecting symbols. We cannot perform the
     * simple checks such as undefined rule until we have collected all
     * tokens and rules from the imported grammars into a single collection.
     */
    public class SemanticPipeline
    {
        public Grammar g;

        public SemanticPipeline(Grammar g)
        {
            this.g = g;
        }

        public virtual void Process()
        {
            if (g.ast == null)
            {
                return;
            }

            // COLLECT RULE OBJECTS
            RuleCollector ruleCollector = new(g);
            ruleCollector.Process(g.ast);

            // CLONE RULE ASTs FOR CONTEXT REFERENCE
            foreach (Rule rule in ruleCollector.rules.Values)
            {
                IList<RuleAST> list;
                if (!g.contextASTs.TryGetValue(rule.GetBaseContext(), out list) || list == null)
                {
                    list = new List<RuleAST>();
                    g.contextASTs[rule.GetBaseContext()] = list;
                }

                list.Add((RuleAST) rule.ast.DupTree());
            }

            // DO BASIC / EASY SEMANTIC CHECKS
            int prevErrors = g.tool.errMgr.GetNumErrors();
            BasicSemanticChecks basics = new(g, ruleCollector);
            basics.Process();
            if (g.tool.errMgr.GetNumErrors() > prevErrors)
            {
                return;
            }

            // TRANSFORM LEFT-RECURSIVE RULES
            prevErrors = g.tool.errMgr.GetNumErrors();
            LeftRecursiveRuleTransformer lrtrans =
                new(g.ast, ruleCollector.rules.Values, g);
            lrtrans.TranslateLeftRecursiveRules();

            // don't continue if we got errors during left-recursion elimination
            if (g.tool.errMgr.GetNumErrors() > prevErrors)
            {
                return;
            }

            // AUTO LEFT FACTORING
            LeftFactoringRuleTransformer lftrans = new(g.ast, ruleCollector.rules, g);
            lftrans.TranslateLeftFactoredRules();

            // STORE RULES IN GRAMMAR
            foreach (Rule r in ruleCollector.rules.Values)
            {
                g.DefineRule(r);
            }

            // COLLECT SYMBOLS: RULES, ACTIONS, TERMINALS, ...
            SymbolCollector collector = new(g);
            collector.Process(g.ast);

            // CHECK FOR SYMBOL COLLISIONS
            SymbolChecks symcheck = new(g, collector);
            symcheck.Process(); // side-effect: strip away redef'd rules.

            foreach (GrammarAST a in collector.namedActions)
            {
                g.DefineAction(a);
            }

            // LINK (outermost) ALT NODES WITH Alternatives
            foreach (Rule r in g.rules.Values)
            {
                for (int i = 1;
                    i <= r.numberOfAlts;
                    i++)
                {
                    r.alt[i].ast.alt = r.alt[i];
                }
            }

            // ASSIGN TOKEN TYPES
            g.ImportTokensFromTokensFile();
            if (g.IsLexer())
            {
                AssignLexerTokenTypes(g, collector.tokensDefs);
            }
            else
            {
                AssignTokenTypes(g, collector.tokensDefs,
                    collector.tokenIDRefs, collector.terminals);
            }

            symcheck.CheckForModeConflicts(g);

            AssignChannelTypes(g, collector.channelDefs);

            // CHECK RULE REFS NOW (that we've defined rules in grammar)
            symcheck.CheckRuleArgs(g, collector.rulerefs);
            IdentifyStartRules(collector);
            symcheck.CheckForQualifiedRuleIssues(g, collector.qualifiedRulerefs);

            // don't continue if we got symbol errors
            if (g.tool.GetNumErrors() > 0)
            {
                return;
            }

            // CHECK ATTRIBUTE EXPRESSIONS FOR SEMANTIC VALIDITY
            AttributeChecks.CheckAllAttributeExpressions(g);

            UseDefAnalyzer.TrackTokenRuleRefsInActions(g);
        }

        internal virtual void IdentifyStartRules(SymbolCollector collector)
        {
            foreach (GrammarAST @ref in collector.rulerefs)
            {
                string ruleName = @ref.Text;
                Rule r = g.GetRule(ruleName);
                if (r != null)
                {
                    r.isStartRule = false;
                }
            }
        }

        internal virtual void AssignLexerTokenTypes(Grammar g, IList<GrammarAST> tokensDefs)
        {
            Grammar G = g.GetOutermostGrammar(); // put in root, even if imported
            foreach (GrammarAST def in tokensDefs)
            {
                // tokens { id (',' id)* } so must check IDs not TOKEN_REF
                if (Grammar.IsTokenName(def.Text))
                {
                    G.DefineTokenName(def.Text);
                }
            }

            /* Define token types for nonfragment rules which do not include a 'type(...)'
             * or 'more' lexer command.
             */
            foreach (Rule r in g.rules.Values)
            {
                if (!r.IsFragment() && !HasTypeOrMoreCommand(r))
                {
                    G.DefineTokenName(r.name);
                }
            }

            // FOR ALL X : 'xxx'; RULES, DEFINE 'xxx' AS TYPE X
            IList<Tuple<GrammarAST, GrammarAST>> litAliases = Grammar.GetStringLiteralAliasesFromLexerRules(g.ast);
            ISet<string> conflictingLiterals = new HashSet<string>();
            if (litAliases != null)
            {
                foreach (Tuple<GrammarAST, GrammarAST> pair in litAliases)
                {
                    GrammarAST nameAST = pair.Item1;
                    GrammarAST litAST = pair.Item2;
                    if (!G.stringLiteralToTypeMap.ContainsKey(litAST.Text))
                    {
                        G.DefineTokenAlias(nameAST.Text, litAST.Text);
                    }
                    else
                    {
                        // oops two literal defs in two rules (within or across modes).
                        conflictingLiterals.Add(litAST.Text);
                    }
                }

                foreach (string lit in conflictingLiterals)
                {
                    // Remove literal if repeated across rules so it's not
                    // found by parser grammar.
                    int value;
                    if (G.stringLiteralToTypeMap.TryGetValue(lit, out value))
                    {
                        G.stringLiteralToTypeMap.Remove(lit);
                        if (value > 0 && value < G.typeToStringLiteralList.Count && lit.Equals(G.typeToStringLiteralList[value]))
                        {
                            G.typeToStringLiteralList[value] = null;
                        }
                    }
                }
            }
        }

        internal virtual bool HasTypeOrMoreCommand([NotNull] Rule r)
        {
            GrammarAST ast = r.ast;
            if (ast == null)
            {
                return false;
            }

            GrammarAST altActionAst = (GrammarAST) ast.GetFirstDescendantWithType(ANTLRParser.LEXER_ALT_ACTION);
            if (altActionAst == null)
            {
                // the rule isn't followed by any commands
                return false;
            }

            // first child is the alt itself, subsequent are the actions
            for (int i = 1;
                i < altActionAst.ChildCount;
                i++)
            {
                GrammarAST node = (GrammarAST) altActionAst.GetChild(i);
                if (node.Type == ANTLRParser.LEXER_ACTION_CALL)
                {
                    if ("type".Equals(node.GetChild(0).Text))
                    {
                        return true;
                    }
                }
                else if ("more".Equals(node.Text))
                {
                    return true;
                }
            }

            return false;
        }

        internal virtual void AssignTokenTypes(Grammar g, IList<GrammarAST> tokensDefs,
            IList<GrammarAST> tokenIDs, IList<GrammarAST> terminals)
        {
            //Grammar G = g.getOutermostGrammar(); // put in root, even if imported

            // create token types for tokens { A, B, C } ALIASES
            foreach (GrammarAST alias in tokensDefs)
            {
                if (g.GetTokenType(alias.Text) != TokenConstants.InvalidType)
                {
                    g.tool.errMgr.GrammarError(ErrorType.TOKEN_NAME_REASSIGNMENT, g.fileName, alias.Token, alias.Text);
                }

                g.DefineTokenName(alias.Text);
            }

            // DEFINE TOKEN TYPES FOR TOKEN REFS LIKE ID, INT
            foreach (GrammarAST idAST in tokenIDs)
            {
                if (g.GetTokenType(idAST.Text) == TokenConstants.InvalidType)
                {
                    g.tool.errMgr.GrammarError(ErrorType.IMPLICIT_TOKEN_DEFINITION, g.fileName, idAST.Token, idAST.Text);
                }

                g.DefineTokenName(idAST.Text);
            }

            // VERIFY TOKEN TYPES FOR STRING LITERAL REFS LIKE 'while', ';'
            foreach (GrammarAST termAST in terminals)
            {
                if (termAST.Type != ANTLRParser.STRING_LITERAL)
                {
                    continue;
                }

                if (g.GetTokenType(termAST.Text) == TokenConstants.InvalidType)
                {
                    g.tool.errMgr.GrammarError(ErrorType.IMPLICIT_STRING_DEFINITION, g.fileName, termAST.Token, termAST.Text);
                }
            }

            g.tool.Log("semantics", "tokens=" + g.tokenNameToTypeMap);
            g.tool.Log("semantics", "strings=" + g.stringLiteralToTypeMap);
        }

        /**
         * Assign constant values to custom channels defined in a grammar.
         * 
         * @param g The grammar.
         * @param channelDefs A collection of AST nodes defining individual channels
         * within a {@code channels{}} block in the grammar.
         */
        internal virtual void AssignChannelTypes(Grammar g, IList<GrammarAST> channelDefs)
        {
            Grammar outermost = g.GetOutermostGrammar();
            foreach (GrammarAST channel in channelDefs)
            {
                string channelName = channel.Text;

                // Channel names can't alias tokens or modes, because constant
                // values are also assigned to them and the ->channel(NAME) lexer
                // command does not distinguish between the various ways a constant
                // can be declared. This method does not verify that channels do not
                // alias rules, because rule names are not associated with constant
                // values in ANTLR grammar semantics.

                if (g.GetTokenType(channelName) != TokenConstants.InvalidType)
                {
                    g.tool.errMgr.GrammarError(ErrorType.CHANNEL_CONFLICTS_WITH_TOKEN, g.fileName, channel.Token, channelName);
                }

                if (LexerATNFactory.COMMON_CONSTANTS.ContainsKey(channelName))
                {
                    g.tool.errMgr.GrammarError(ErrorType.CHANNEL_CONFLICTS_WITH_COMMON_CONSTANTS, g.fileName, channel.Token, channelName);
                }

                if (outermost is LexerGrammar)
                {
                    LexerGrammar lexerGrammar = (LexerGrammar) outermost;
                    if (lexerGrammar.modes.ContainsKey(channelName))
                    {
                        g.tool.errMgr.GrammarError(ErrorType.CHANNEL_CONFLICTS_WITH_MODE, g.fileName, channel.Token, channelName);
                    }
                }

                outermost.DefineChannelName(channel.Text);
            }
        }
    }
}