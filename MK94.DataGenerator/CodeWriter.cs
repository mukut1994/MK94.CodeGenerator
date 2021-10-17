using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK94.DataGenerator
{
    public enum IndentStyle
    {
        NewLine,
        SameLine
    }

    public class CodeBuilder
    {
        private static readonly List<string> filesWritten = new List<string>();

        private readonly string project;
        private readonly StreamWriter output;
        private readonly IndentStyle indentStyle;
        private readonly StringBuilder lineBuilder = new StringBuilder();

        private bool lineHasContent = false;
        private int indent = 0;
        private int enabledCount = 0;
        private int parenthesisOpenCount = 0;
        private bool optionalComma = false;

        private bool BuilderEnabled => enabledCount > 0;

        public static Func<string, CodeBuilder> FactoryFromBasePath(string path, IndentStyle indentStyle = IndentStyle.NewLine)
        {
            return x => FromFile(Path.Combine(path, x));
        }

        public static CodeBuilder FromMemoryStream(out MemoryStream stream, IndentStyle indentStyle = IndentStyle.NewLine)
        {
            stream = new MemoryStream();

            return new CodeBuilder(new StreamWriter(stream), null, indentStyle);
        }

        public static CodeBuilder FromFile(string file, IndentStyle indentStyle = IndentStyle.NewLine)
        {
            if (File.Exists(file))
                File.Delete(file);

            if (!Directory.Exists(Path.GetDirectoryName(file)))
                Directory.CreateDirectory(Path.GetDirectoryName(file));

            filesWritten.Add(file);

            return new CodeBuilder(new StreamWriter(File.OpenWrite(file)), null, indentStyle);
        }

        public static void GenerateGitIgnores()
        {
            var groupByDir = filesWritten.GroupBy(x => Path.GetDirectoryName(x));

            foreach (var group in groupByDir)
            {
                var file = Path.Combine(group.Key, ".gitignore");

                if (File.Exists(file))
                    File.Delete(file);

                var output = new StreamWriter(File.OpenWrite(file));

                foreach (var item in group)
                    output.WriteLine(Path.GetFileName(item));

                output.Close();
            }
        }

        private CodeBuilder(StreamWriter output, string project, IndentStyle indentStyle = IndentStyle.NewLine)
        {
            this.output = output;
            this.project = project;
            this.indentStyle = indentStyle;
        }

        public CodeBuilder Flush()
        {
            output.Flush();

            return this;
        }

        private void InternalAppend(string content, bool force)
        {
            if (!BuilderEnabled && !force)
                return;

            if (!lineHasContent)
                lineBuilder.Append("".PadLeft(indent * 4, ' '));

            if (optionalComma)
            {
                optionalComma = false;
                AppendComma();
            }

            if (content.Contains(Environment.NewLine))
            {
                var lines = content.Split(Environment.NewLine);

                for(int i = 0; i < lines.Length; i++)
                {
                    Append(lines[i]);

                    if (i + 1 != lines.Length)
                        NewLine();
                }
            }
            else
            {
                lineHasContent = true;
                lineBuilder.Append(content);
            }
        }

        public CodeBuilder Enable()
        {
            enabledCount++;
            return this;
        }

        public CodeBuilder AppendOptionalComma()
        {
            optionalComma = true;
            return this;
        }

        public CodeBuilder NewLine(bool force = false)
        {
            if (!BuilderEnabled && !force)
                return this;

            lineHasContent = false;
            output.WriteLine(lineBuilder);
            lineBuilder.Clear();

            return this;
        }

        public CodeBuilder AppendLine(string content, bool force = false)
        {
            InternalAppend(content, force);
            NewLine(force);

            return this;
        }

        public CodeBuilder Append(string content, bool force = false)
        {
            if (!BuilderEnabled && !force)
                return this;

            InternalAppend(content, force);

            return this;
        }

        public CodeBuilder Append(Action<CodeBuilder> generator, bool force = false)
        {
            if (!BuilderEnabled && !force)
                return this;

            generator(this);

            return this;
        }

        public CodeBuilder Append<T>(Action<CodeBuilder, T> generator, T from, bool force = false)
        {
            if (!BuilderEnabled && !force)
                return this;

            generator(this, from);

            return this;
        }

        public CodeBuilder Append<T>(Action<CodeBuilder, T> generator, IEnumerable<T> from, bool force = false)
        {
            if (!BuilderEnabled && !force)
                return this;

            foreach(var f in from)
                generator(this, f);

            return this;
        }

        public CodeBuilder AppendComma(bool force = false)
        {
            Append(", ", force);

            if (parenthesisOpenCount > 0 && lineBuilder.Length > 150 + indent * 4)
                NewLine(force);

            return this;
        }

        public CodeBuilder WithParenthesis(Action<CodeBuilder> blockContent)
        {
            return this.OpenParanthesis().Append(blockContent).CloseParanthesis();
        }

        public CodeBuilder WithParenthesis<T>(Action<CodeBuilder, T> blockContent, T item)
        {
            return this.OpenParanthesis().Append(blockContent, item).CloseParanthesis();
        }

        public CodeBuilder WithParenthesis<T>(Action<CodeBuilder, T> blockContent, IEnumerable<T> item)
        {
            return this.OpenParanthesis().Append<T>((b, i) => b.Append(blockContent, i).AppendOptionalComma(), item).CloseParanthesis();
        }

        public CodeBuilder OpenParanthesis(bool force = false)
        {
            optionalComma = false;
            parenthesisOpenCount++;
            indent++;
            InternalAppend("(", force);

            return this;
        }

        public CodeBuilder CloseParanthesis(bool force = false)
        {
            optionalComma = false;
            parenthesisOpenCount--;

            if (parenthesisOpenCount < 0)
                throw new InvalidOperationException("Closing too many parenthesis");

            InternalAppend(")", force);
            indent--;

            return this;
        }

        public CodeBuilder IncreaseIndent()
        {
            indent++;
            return this;
        }

        public CodeBuilder DecreaseIndent()
        {
            indent--;
            return this;
        }

        public CodeBuilder OpenBlock(bool force = false)
        {
            optionalComma = false;
            if (indentStyle == IndentStyle.NewLine && lineHasContent)
            {
                NewLine(force);
                InternalAppend("{", force);
            }
            else if (indentStyle == IndentStyle.SameLine)
            {
                InternalAppend(" {", force);
            }
            else
                InternalAppend("{", force);

            indent++;

            NewLine(force);

            return this;
        }

        public CodeBuilder WithBlock(Action<CodeBuilder> blockContent)
        {
            return this.OpenBlock().Append(blockContent).CloseBlock();
        }

        public CodeBuilder WithBlock<T>(Action<CodeBuilder, T> blockContent, T item)
        {
            return this.OpenBlock().Append(blockContent, item).CloseBlock();
        }

        public CodeBuilder WithBlock<T>(Action<CodeBuilder, T> blockContent, IEnumerable<T> item)
        {
            return this.OpenBlock().Append(blockContent, item).CloseBlock();
        }

        public CodeBuilder CloseBlock(bool force = false, bool sameLine = false)
        {
            optionalComma = false;
            indent--;

            if (indent < 0)
                throw new InvalidOperationException("Closing too many blocks");

            if (lineHasContent)
                NewLine(force);
            else
            {
                lineBuilder.Clear();
            }

            InternalAppend("}", force);

            if (!sameLine)
                NewLine(force);

            return this;
        }

        public bool EnableProjects(Type type)
        {
            var attribute = type.GetCustomAttributes(false).Select(a => a as ProjectAttribute).FirstOrDefault(a => a?.Project.Equals(project) == true);

            if (attribute != null)
            {
                enabledCount++;
                return true;
            }

            return false;
        }

        public void DisableProjects(bool anyEnabled)
        {
            if (anyEnabled)
                enabledCount--;
        }
    }
}
