// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Antlr4.Build.Tasks
{
    public class Antlr4ClassGenerationTask
        : Task
    {
        private const string DefaultGeneratedSourceExtension = "g4";
        private List<ITaskItem> _generatedCodeFiles = new();

        public Antlr4ClassGenerationTask()
        {
            GeneratedSourceExtension = DefaultGeneratedSourceExtension;
        }

        [Required] public string ToolPath { get; set; }

        [Required] public string OutputPath { get; set; }

        public string Encoding { get; set; }

        public string TargetLanguage { get; set; }

        public string TargetFrameworkVersion { get; set; }

        public string BuildTaskPath { get; set; }

        public ITaskItem[] SourceCodeFiles { get; set; }

        public ITaskItem[] TokensFiles { get; set; }

        public ITaskItem[] AbstractGrammarFiles { get; set; }

        public string GeneratedSourceExtension { get; set; }

        public string TargetNamespace { get; set; }

        public string[] LanguageSourceExtensions { get; set; }

        public bool GenerateListener { get; set; }

        public bool GenerateVisitor { get; set; }

        public bool ForceAtn { get; set; }

        public bool AbstractGrammar { get; set; }

        [Required] public string JavaVendor { get; set; }

        [Required] public string JavaInstallation { get; set; }

        public string JavaExecutable { get; set; }

        public bool UseCSharpGenerator { get; set; }

        [Output]
        public ITaskItem[] GeneratedCodeFiles
        {
            get => _generatedCodeFiles.ToArray();
            set => _generatedCodeFiles = new List<ITaskItem>(value);
        }

        public override bool Execute()
        {
            bool success;

            if (!Path.IsPathRooted(ToolPath))
            {
                ToolPath = Path.Combine(Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode), ToolPath);
            }

            if (!Path.IsPathRooted(BuildTaskPath))
            {
                BuildTaskPath = Path.Combine(Path.GetDirectoryName(BuildEngine.ProjectFileOfTaskNode), BuildTaskPath);
            }

            try
            {
                AntlrClassGenerationTaskInternal wrapper = CreateBuildTaskWrapper();
                success = wrapper.Execute();

                if (success)
                {
                    _generatedCodeFiles.AddRange(wrapper.GeneratedCodeFiles.Select(file => (ITaskItem) new TaskItem(file)));
                }

                foreach (BuildMessage message in wrapper.BuildMessages)
                {
                    ProcessBuildMessage(message);
                }
            }
            catch (Exception exception)
            {
                if (IsFatalException(exception))
                {
                    throw;
                }

                ProcessExceptionAsBuildMessage(exception);
                success = false;
            }

            return success;
        }

        private void ProcessExceptionAsBuildMessage(Exception exception)
        {
            ProcessBuildMessage(new BuildMessage(exception.Message));
        }

        private void ProcessBuildMessage(BuildMessage message)
        {
            string logMessage;
            string errorCode;
            errorCode = Log.ExtractMessageCode(message.Message, out logMessage);
            if (String.IsNullOrEmpty(errorCode))
            {
                if (message.Message.StartsWith("Executing command:", StringComparison.Ordinal) && message.Severity == TraceLevel.Info)
                {
                    // This is a known informational message
                    logMessage = message.Message;
                }
                else
                {
                    errorCode = "AC1000";
                    logMessage = "Unknown build error: " + message.Message;
                }
            }

            string subcategory = null;
            string helpKeyword = null;

            switch (message.Severity)
            {
                case TraceLevel.Error:
                    Log.LogError(subcategory, errorCode, helpKeyword, message.FileName, message.LineNumber, message.ColumnNumber, 0, 0, logMessage);
                    break;
                case TraceLevel.Warning:
                    Log.LogWarning(subcategory, errorCode, helpKeyword, message.FileName, message.LineNumber, message.ColumnNumber, 0, 0, logMessage);
                    break;
                case TraceLevel.Info:
                    Log.LogMessage(MessageImportance.Normal, logMessage);
                    break;
                case TraceLevel.Verbose:
                    Log.LogMessage(MessageImportance.Low, logMessage);
                    break;
            }
        }

        private AntlrClassGenerationTaskInternal CreateBuildTaskWrapper()
        {
            AntlrClassGenerationTaskInternal wrapper = new();

            IList<string> sourceCodeFiles = null;
            if (SourceCodeFiles != null)
            {
                sourceCodeFiles = new List<string>(SourceCodeFiles.Length);
                foreach (ITaskItem taskItem in SourceCodeFiles)
                {
                    sourceCodeFiles.Add(taskItem.ItemSpec);
                }
            }

            if (TokensFiles != null && TokensFiles.Length > 0)
            {
                Directory.CreateDirectory(OutputPath);

                var copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ITaskItem taskItem in TokensFiles)
                {
                    string fileName = taskItem.ItemSpec;
                    if (!File.Exists(fileName))
                    {
                        Log.LogError("The tokens file '{0}' does not exist.", fileName);
                        continue;
                    }

                    string vocabName = Path.GetFileNameWithoutExtension(fileName);
                    if (!copied.Add(vocabName))
                    {
                        Log.LogWarning("The tokens file '{0}' conflicts with another tokens file in the same project.", fileName);
                        continue;
                    }

                    string target = Path.Combine(OutputPath, Path.GetFileName(fileName));
                    if (!Path.GetExtension(target).Equals(".tokens", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogError("The destination for the tokens file '{0}' did not have the correct extension '.tokens'.", target);
                        continue;
                    }

                    File.Copy(fileName, target, true);
                    File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
                }
            }

            wrapper.ToolPath = ToolPath;
            wrapper.SourceCodeFiles = sourceCodeFiles;
            wrapper.TargetLanguage = TargetLanguage;
            wrapper.TargetFrameworkVersion = TargetFrameworkVersion;
            wrapper.OutputPath = OutputPath;
            wrapper.Encoding = Encoding;
            wrapper.LanguageSourceExtensions = LanguageSourceExtensions;
            wrapper.TargetNamespace = TargetNamespace;
            wrapper.GenerateListener = GenerateListener;
            wrapper.GenerateVisitor = GenerateVisitor;
            wrapper.ForceAtn = ForceAtn;
            wrapper.AbstractGrammar = AbstractGrammar;
            wrapper.JavaVendor = JavaVendor;
            wrapper.JavaInstallation = JavaInstallation;
            wrapper.JavaExecutable = JavaExecutable;
            wrapper.UseCSharpGenerator = UseCSharpGenerator;
            return wrapper;
        }

        internal static bool IsFatalException(Exception exception)
        {
            while (exception != null)
            {
                if (exception is OutOfMemoryException)
                {
                    return true;
                }

                if (!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return false;
        }
    }
}