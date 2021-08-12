using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Utility;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace Antlr4.Runtime.Test
{
    internal static class Extensions
    {
        public static void DumpDFA(this Parser parser)
        {
            bool seenOne = false;
            for (int d = 0;
                d < parser.Interpreter.atn.decisionToDFA.Length;
                d++)
            {
                DFA dfa = parser.Interpreter.atn.decisionToDFA[d];
                if (!dfa.IsEmpty)
                {
                    if (seenOne)
                    {
                        Console.Out.WriteLine();
                    }

                    Console.Out.WriteLine("Decision " + dfa.decision + ":");
                    Console.Out.Write(dfa.ToString(parser.Vocabulary, parser.RuleNames));
                    seenOne = true;
                }
            }
        }
    }

    /// <summary>
    ///     This is a duplicate of <c>ConsoleErrorListener&lt;Symbol&gt;</c> which is used for testing the portable
    ///     runtimes.
    /// </summary>
    public class TestConsoleErrorListener<Symbol> : IAntlrErrorListener<Symbol>
    {
        public static readonly TestConsoleErrorListener<Symbol> Instance = new();

        public virtual void SyntaxError(IRecognizer recognizer, Symbol offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Console.Error.WriteLine("line " + line + ":" + charPositionInLine + " " + msg);
        }
    }

    public abstract class BaseTest
    {
        public string tmpdir;

        /**
         * If error during parser execution, store stderr here; can't return
         * stdout and stderr.  This doesn't trap errors from running antlr.
         */
        protected string stderrDuringParse;

        public TestContext TestContext { get; set; }

        [TestCleanup]
        public void TestCleanup()
        {
            if (TestContext.CurrentTestOutcome == UnitTestOutcome.Passed)
            {
                // remove tmpdir if no error.
                eraseTempDir();
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // new output dir for each test
            string tempTestFolder = GetType().Name + "-" + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            string dir = Path.Combine(Path.GetTempPath(), tempTestFolder);
            if (Directory.Exists(dir))
            {
                throw new InvalidOperationException();
            }

            tmpdir = dir;
        }

        protected static string WindowsFolder
        {
            get
            {
#if NET40PLUS
                return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
#else
                string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
                return Path.GetDirectoryName(systemFolder);
#endif
            }
        }

        protected static string UserProfile
        {
            get
            {
#if NET40PLUS
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
#else
                string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.GetDirectoryName(documentsFolder);
#endif
            }
        }

        protected static string PathCombine(params string[] paths)
        {
#if NET40PLUS
            return Path.Combine(paths);
#else
            string result = paths[0];
            for (int i = 1;
                i < paths.Length;
                i++)
            {
                result = Path.Combine(result, paths[i]);
            }

            return result;
#endif
        }

        private void ExpectConsole(string expectedOutput, string expectedErrors, Action testFunction)
        {
            TextWriter outWriter = Console.Out;
            TextWriter errorWriter = Console.Error;

            StringWriter captureOut = new StringWriter();
            StringWriter captureError = new StringWriter();

            try
            {
                Console.SetOut(captureOut);
                Console.SetError(captureError);
                testFunction();
            }
            finally
            {
                Console.SetOut(outWriter);
                Console.SetError(errorWriter);
            }

            captureOut.Flush();
            captureError.Flush();
            string output = captureOut.ToString().Replace("\r", String.Empty);
            string errors = captureError.ToString().Replace("\r", String.Empty);

            // Fixup for small behavioral difference at EOF...
            if (output.Length == expectedOutput.Length - 1 && output[output.Length - 1] != '\n')
            {
                output += "\n";
            }

            Assert.AreEqual(expectedOutput, output);
            Assert.AreEqual(expectedErrors, errors);
        }

        private class TreeShapeListener : IParseTreeListener
        {
            public void EnterEveryRule(ParserRuleContext context)
            {
                for (int i = 0;
                    i < context.ChildCount;
                    i++)
                {
                    IParseTree parent = context.GetChild(i).Parent;
                    if (!(parent is IRuleNode) || ((IRuleNode) parent).RuleContext != context)
                    {
                        throw new Exception("Invalid parse tree shape detected.");
                    }
                }
            }

            public void ExitEveryRule(ParserRuleContext ctx)
            {
            }

            public void VisitErrorNode(IErrorNode node)
            {
            }

            public void VisitTerminal(ITerminalNode node)
            {
            }
        }

        internal void LexerTest(LexerTestOptions options)
        {
            ICharStream inputStream = new AntlrInputStream(options.Input.Replace("\r", String.Empty));
            Lexer lex = options.Lexer(inputStream);
#if PORTABLE
            lex.AddErrorListener(TestConsoleErrorListener<int>.Instance);
#endif
            CommonTokenStream tokens = new CommonTokenStream(lex);
            ExpectConsole(
                options.ExpectedOutput.Replace("\r", String.Empty),
                options.ExpectedErrors.Replace("\r", String.Empty),
                () =>
                {
                    tokens.Fill();
                    foreach (IToken token in tokens.GetTokens())
                    {
                        Console.WriteLine(token.ToString());
                    }

                    if (options.ShowDFA)
                    {
                        Console.Write(lex.Interpreter.GetDFA(Lexer.DefaultMode).ToLexerString());
                    }
                });
        }

        internal void ParserTest<TParser>(ParserTestOptions<TParser> options)
            where TParser : Parser
        {
            ICharStream inputStream = new AntlrInputStream(options.Input.Replace("\r", String.Empty));
            Lexer lex = options.Lexer(inputStream);
            CommonTokenStream tokens = new CommonTokenStream(lex);
            TParser parser = options.Parser(tokens);
#if PORTABLE
            lex.AddErrorListener(TestConsoleErrorListener<int>.Instance);
            parser.AddErrorListener(TestConsoleErrorListener<IToken>.Instance);
#endif
            if (options.Debug)
            {
                parser.Interpreter.reportAmbiguities = true;
                parser.AddErrorListener(new DiagnosticErrorListener());
            }

            parser.BuildParseTree = true;

            ExpectConsole(
                options.ExpectedOutput.Replace("\r", String.Empty),
                options.ExpectedErrors.Replace("\r", String.Empty),
                () =>
                {
                    IParseTree tree = options.ParserStartRule(parser);
                    ParseTreeWalker.Default.Walk(new TreeShapeListener(), tree);
                });
        }

        /**
         * Wow! much faster than compiling outside of VM. Finicky though.
         * Had rules called r and modulo. Wouldn't compile til I changed to 'a'.
         */
        protected virtual bool compile(params string[] fileNames)
        {
            DirectoryInfo outputDir = new(tmpdir);
            try
            {
                string compiler = PathCombine(WindowsFolder, "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");

                var args = new List<string>();
                args.AddRange(getCompileOptions());

                bool hasTestClass = false;
                foreach (string fileName in fileNames)
                {
                    if (fileName.Equals("Test.cs"))
                    {
                        hasTestClass = true;
                    }

                    if (fileName.EndsWith(".dll"))
                    {
                        args.Add("/reference:" + fileName);
                    }
                    else
                    {
                        args.Add(fileName);
                    }
                }

                if (hasTestClass)
                {
                    args.Insert(1, "/target:exe");
                    args.Insert(1, "/reference:Parser.dll");
                    args.Insert(1, "/out:Test.exe");
                }
                else
                {
                    args.Insert(1, "/target:library");
                    args.Insert(1, "/out:Parser.dll");
                }

                Process process = Process.Start(new ProcessStartInfo(compiler, '"' + Utils.Join("\" \"", args) + '"')
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = tmpdir
                });

                StreamVacuum stdout = new(process.StandardOutput);
                StreamVacuum stderr = new(process.StandardError);
                stdout.start();
                stderr.start();
                process.WaitForExit();
                stdout.join();
                stderr.join();
                if (stdout.ToString().Length > 0)
                {
                    Console.Error.WriteLine("compile stdout from: " + Utils.Join(" ", args));
                    Console.Error.WriteLine(stdout);
                }

                if (stderr.ToString().Length > 0)
                {
                    Console.Error.WriteLine("compile stderr from: " + Utils.Join(" ", args));
                    Console.Error.WriteLine(stderr);
                }

                int ret = process.ExitCode;
                return ret == 0;
            }
            catch (Exception)
            {
                Console.Error.WriteLine("can't exec compilation");
                //e.printStackTrace(System.err);
                return false;
            }
        }

        public virtual IList<string> getCompileOptions()
        {
            IList<string> compileOptions = new List<string>();
            compileOptions.Add("/debug");
            compileOptions.Add("/warn:4");
            compileOptions.Add("/nologo");
            compileOptions.Add("/reference:" + typeof(Lexer).Assembly.Location);
            return compileOptions;
        }

        protected virtual string JavaHome
        {
            get
            {
                string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
#if NETFRAMEWORK
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(javaKey))
#else
                using (var baseKey = Registry.LocalMachine.OpenSubKey(javaKey))
#endif
                {
                    string currentVersion = baseKey.GetValue("CurrentVersion").ToString();
                    using (RegistryKey homeKey = baseKey.OpenSubKey(currentVersion))
                    {
                        return homeKey.GetValue("JavaHome").ToString();
                    }
                }
            }
        }

        protected virtual string MavenHome
        {
            get
            {
                string mavenHome = Environment.GetEnvironmentVariable("M2_HOME");
                if (!Directory.Exists(mavenHome))
                {
                    mavenHome = Path.Combine(UserProfile, ".m2");
                }

                return mavenHome;
            }
        }

        protected virtual string GetMavenArtifact(string groupId, string artifactId, string version, string classifier = null)
        {
            string folder = PathCombine(MavenHome, "repository", groupId.Replace('.', Path.DirectorySeparatorChar), artifactId, version);
            string fileNameFormat = String.IsNullOrEmpty(classifier) ? "{0}-{1}.jar" : "{0}-{1}-{2}.jar";
            string fileName = String.Format(fileNameFormat, artifactId, version, classifier);
            return Path.Combine(folder, fileName);
        }

        /**
         * Return true if all is ok, no errors
         */
        protected virtual bool antlr(string fileName, string grammarFileName, string grammarStr, bool defaultListener, params string[] extraOptions)
        {
            mkdir(tmpdir);
            writeFile(tmpdir, fileName, grammarStr);
            try
            {
                string compiler = PathCombine(JavaHome, "bin", "java.exe");

                var classpath = new List<string>();
                classpath.Add(GetMavenArtifact("com.tunnelvisionlabs", "antlr4-csharp", "4.3-SNAPSHOT"));
                classpath.Add(GetMavenArtifact("com.tunnelvisionlabs", "antlr4-runtime", "4.3"));
                classpath.Add(GetMavenArtifact("com.tunnelvisionlabs", "antlr4", "4.3"));
                classpath.Add(GetMavenArtifact("org.antlr", "antlr-runtime", "3.5.2"));
                classpath.Add(GetMavenArtifact("org.antlr", "ST4", "4.0.8"));

                var options = new List<string>();
                options.Add("-cp");
                options.Add(Utils.Join(";", classpath));
                options.Add("org.antlr.v4.Tool");

                options.AddRange(extraOptions);
                options.Add("-o");
                options.Add(tmpdir);
                options.Add("-lib");
                options.Add(tmpdir);

#if NET5_0
                options.Add("-Dlanguage=CSharp_v5_0");
#elif NETSTANDARD || NETCOREAPP
                options.Add("-Dlanguage=CSharp_v4_5");
#elif NETFRAMEWORK
                options.Add("-Dlanguage=CSharp_v4_5");
#else
#error Unknown assembly.
#endif

                options.Add(grammarFileName);

                Process process = Process.Start(new ProcessStartInfo(compiler, '"' + Utils.Join("\" \"", options) + '"')
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = tmpdir
                });

                StreamVacuum stdout = new(process.StandardOutput);
                StreamVacuum stderr = new(process.StandardError);
                stdout.start();
                stderr.start();
                process.WaitForExit();
                stdout.join();
                stderr.join();
                if (stdout.ToString().Length > 0)
                {
                    Console.Error.WriteLine("compile stdout from: " + Utils.Join(" ", options));
                    Console.Error.WriteLine(stdout);
                }

                if (stderr.ToString().Length > 0)
                {
                    Console.Error.WriteLine("compile stderr from: " + Utils.Join(" ", options));
                    Console.Error.WriteLine(stderr);
                }

                int ret = process.ExitCode;
                return ret == 0;
            }
            catch (Exception)
            {
                Console.Error.WriteLine("can't exec compilation");
                //e.printStackTrace(System.err);
                return false;
            }
        }

        /**
         * Return true if all is well
         */
        protected virtual bool rawGenerateAndBuildRecognizer(string grammarFileName,
            string grammarStr,
            [MaybeNull] string parserName,
            string lexerName,
            params string[] extraOptions)
        {
            return rawGenerateAndBuildRecognizer(grammarFileName, grammarStr, parserName, lexerName, false, extraOptions);
        }

        /**
         * Return true if all is well
         */
        protected virtual bool rawGenerateAndBuildRecognizer(string grammarFileName,
            string grammarStr,
            [MaybeNull] string parserName,
            string lexerName,
            bool defaultListener,
            params string[] extraOptions)
        {
            bool allIsWell =
                antlr(grammarFileName, grammarFileName, grammarStr, defaultListener, extraOptions);
            if (!allIsWell)
            {
                return false;
            }

            var files = new List<string>();
            if (lexerName != null)
            {
                files.Add(lexerName + ".cs");
            }

            if (parserName != null)
            {
                files.Add(parserName + ".cs");
                if (Array.IndexOf(extraOptions, "-no-listener") >= 0)
                {
                    files.Add(grammarFileName.Substring(0, grammarFileName.LastIndexOf('.')) + "BaseListener.cs");
                    files.Add(grammarFileName.Substring(0, grammarFileName.LastIndexOf('.')) + "Listener.cs");
                }

                if (Array.IndexOf(extraOptions, "-visitor") >= 0)
                {
                    files.Add(grammarFileName.Substring(0, grammarFileName.LastIndexOf('.')) + "BaseVisitor.cs");
                    files.Add(grammarFileName.Substring(0, grammarFileName.LastIndexOf('.')) + "Visitor.cs");
                }
            }

            allIsWell = compile(files.ToArray());
            return allIsWell;
        }

        public class StreamVacuum
        {
            private readonly StringBuilder buf = new();
            private readonly TextReader @in;
            private Thread sucker;

            public StreamVacuum(TextReader @in)
            {
                this.@in = @in;
            }

            public void start()
            {
                sucker = new Thread(run);
                sucker.Start();
            }

            public void run()
            {
                try
                {
                    string line = @in.ReadLine();
                    while (line != null)
                    {
                        buf.AppendLine(line);
                        line = @in.ReadLine();
                    }
                }
                catch (IOException)
                {
                    Console.Error.WriteLine("can't read output from process");
                }
            }

            /**
             * wait for the thread to finish
             */
            public void join() /*throws InterruptedException*/
            {
                sucker.Join();
            }

            public override string ToString()
            {
                return buf.ToString();
            }
        }

        public static void writeFile(string dir, string fileName, string content)
        {
            File.WriteAllText(Path.Combine(dir, fileName), content);
        }

        protected void mkdir(string dir)
        {
            Directory.CreateDirectory(dir);
        }

        protected virtual void eraseTempDir()
        {
            if (!Path.GetTempPath().Equals(Path.GetDirectoryName(tmpdir) + Path.DirectorySeparatorChar))
            {
                throw new InvalidOperationException();
            }

            if (Directory.Exists(tmpdir))
            {
                Directory.Delete(tmpdir, true);
            }
        }
    }
}