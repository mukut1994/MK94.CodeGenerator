
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System;
using System.Linq;

namespace MK94.CodeGenerator;

public enum IndentStyle
{
    NewLine,
    SameLine
}

public class CodeBuilder
{
    private record OutputContext(string Path, MemoryStream Stream, StreamWriter writer, SHA256 Hash);

    private static readonly List<OutputContext> files = new();

    private readonly StreamWriter output;
    private readonly IndentStyle indentStyle;
    private readonly StringBuilder lineBuilder = new StringBuilder();

    private bool lineHasContent = false;
    private int indent = 0;
    private int parenthesisOpenCount = 0;
    private bool optionalComma = false;
    private bool optionalSpace = false;

    public static Func<string, CodeBuilder> FactoryFromBasePath(string path, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        return x => FromFile(Path.Combine(path, x));
    }

    public static Func<string, CodeBuilder> FactoryFromBasePath(string path, string extraPath, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        return x => FromFile(Path.Combine(path, extraPath, x));
    }

    public static Func<string, CodeBuilder> FactoryFromMemoryStream(out Dictionary<string, MemoryStream> files, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        var dict = new Dictionary<string, MemoryStream>();
        files = dict;

        return x =>
        {
            var ret = FromMemoryStream(out var stream, indentStyle);

            dict[x] = stream;

            return ret;
        };
    }

    public static CodeBuilder FromMemoryStream(out MemoryStream stream, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        stream = new MemoryStream();

        return new CodeBuilder(new StreamWriter(stream), indentStyle);
    }

    public static CodeBuilder FromFile(string file, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        if (!Directory.Exists(Path.GetDirectoryName(file)))
            Directory.CreateDirectory(Path.GetDirectoryName(file));

        var mem = new MemoryStream();
        var sha = SHA256.Create();
        var crypto = new CryptoStream(mem, sha, CryptoStreamMode.Write, true);
        var writer = new StreamWriter(crypto);

        files.Add(new(file, mem, writer, sha));

        return new CodeBuilder(writer, indentStyle);
    }

    private static string HashToString(byte[] hash)
    {
        return Convert.ToBase64String(hash!).Replace('/', '-').ToLower();
    }

    private static void SaveExistingFileHashes(Dictionary<string, string> hashes)
    {
        const string hashFile = "../existing files.json";
        var file = Path.Combine(System.Reflection.Assembly.GetEntryAssembly()!.Location, hashFile);

        if (!File.Exists(file))
            File.Delete(file);

        File.WriteAllText(file, JsonSerializer.Serialize(hashes, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Dictionary<string, string> ReadExistingFileHashes()
    {
        const string hashFile = "../existing files.json";
        var file = Path.Combine(System.Reflection.Assembly.GetEntryAssembly()!.Location, hashFile);

        if (!File.Exists(file))
            return new Dictionary<string, string>();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
    }
    public static void FlushAll(bool force = false)
    {
        var existingHashes = force ? new() : ReadExistingFileHashes();
        var toDelete = new HashSet<string>(existingHashes.Keys);
        var updates = 0;

        foreach (var kv in files)
        {
            var file = kv.Path;
            kv.writer.Flush();
            kv.writer.Close();

            toDelete.Remove(file);

            var actualHash = HashToString(kv.Hash.Hash!);

            if (existingHashes.TryGetValue(file, out var existingHash) && existingHash.Equals(actualHash))
                continue;

            existingHashes[file] = actualHash;
            updates++;

            Console.WriteLine($"Updating {file}");

            if (File.Exists(file))
                File.Delete(file);

            if (!Directory.Exists(Path.GetDirectoryName(file)))
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);

            using var fileStream = File.OpenWrite(file);
            kv.Stream.Position = 0;
            kv.Stream.CopyTo(fileStream);
            fileStream.Flush();
        }

        if (updates == 0 && !toDelete.Any())
            return;

        foreach (var del in toDelete)
        {
            if (File.Exists(del))
                File.Delete(del);

            existingHashes.Remove(del);
        }

        SaveExistingFileHashes(existingHashes);
        if (updates > 0) Console.WriteLine($"Updated {updates} files");
        if (toDelete.Any()) Console.WriteLine($"Deleted {toDelete.Count} files");
    }

    public static void GenerateGitIgnores()
    {
        var groupByDir = files.GroupBy(x => Path.GetDirectoryName(x.Path));

        foreach (var group in groupByDir)
        {
            var file = Path.Combine(group.Key!, ".gitignore");

            var output = FromFile(file);

            foreach (var item in group)
                output.AppendLine(Path.GetFileName(item.Path));

            output.Flush();
        }
    }

    private CodeBuilder(StreamWriter output, IndentStyle indentStyle = IndentStyle.NewLine)
    {
        this.output = output;
        this.indentStyle = indentStyle;
    }

    public CodeBuilder Flush()
    {
        if (lineHasContent)
        {
            output.Write(lineBuilder);
            lineHasContent = false;
            lineBuilder.Clear();
        }

        output.Flush();

        return this;
    }

    private void InternalAppend(string content)
    {
        if (!lineHasContent)
        {
            lineHasContent = true;
            lineBuilder.Append("".PadLeft(indent * 4, ' '));
        }

        if (optionalComma)
        {
            optionalComma = false;
            AppendComma();
        }

        if (optionalSpace)
        {
            optionalSpace = false;
            InternalAppend(" ");
        }

        if (content.Contains(Environment.NewLine))
        {
            var lines = content.Split(Environment.NewLine);

            for (int i = 0; i < lines.Length; i++)
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

    public CodeBuilder AppendWord(string word)
    {
        InternalAppend(word);
        optionalSpace = true;

        return this;
    }

    public CodeBuilder AppendOptionalComma()
    {
        optionalComma = true;
        return this;
    }

    public CodeBuilder NewLine()
    {
        lineHasContent = false;
        output.WriteLine(lineBuilder);
        lineBuilder.Clear();

        return this;
    }

    public CodeBuilder AppendAutomaticallyGeneratedFileComment()
    {
        InternalAppend("// <auto-generated/>");
        NewLine();

        return this;
    }

    public CodeBuilder AppendLine(string content)
    {
        InternalAppend(content);
        NewLine();

        return this;
    }

    public CodeBuilder Append(MemoryStream stream)
    {
        using var reader = new StreamReader(stream);

        var text = reader.ReadToEnd();

        InternalAppend(text);

        return this;
    }

    public CodeBuilder Append(string content)
    {
        InternalAppend(content);

        return this;
    }

    public CodeBuilder Append(Action<CodeBuilder> generator)
    {
        generator(this);

        return this;
    }

    public CodeBuilder Append<T>(Action<CodeBuilder, T> generator, T from)
    {
        generator(this, from);

        return this;
    }

    public CodeBuilder Append<T>(Action<CodeBuilder, T> generator, IEnumerable<T> from)
    {
        foreach (var f in from)
            generator(this, f);

        return this;
    }

    public CodeBuilder AppendComma()
    {
        optionalSpace = false;
        Append(", ");

        if (parenthesisOpenCount > 0 && lineBuilder.Length > 150 + indent * 4)
            NewLine();

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

    public CodeBuilder OpenParanthesis()
    {
        optionalSpace = false;
        optionalComma = false;
        parenthesisOpenCount++;
        indent++;
        InternalAppend("(");

        return this;
    }

    public CodeBuilder CloseParanthesis()
    {
        optionalSpace = false;
        optionalComma = false;
        parenthesisOpenCount--;

        if (parenthesisOpenCount < 0)
            throw new InvalidOperationException("Closing too many parenthesis");

        InternalAppend(")");
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

    public CodeBuilder OpenBlock()
    {
        optionalSpace = false;
        optionalComma = false;
        if (indentStyle == IndentStyle.NewLine && lineHasContent)
        {
            NewLine();
            InternalAppend("{");
        }
        else if (indentStyle == IndentStyle.SameLine)
        {
            InternalAppend(" {");
        }
        else
            InternalAppend("{");

        indent++;

        NewLine();

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

    public CodeBuilder CloseBlock()
    {
        optionalSpace = false;
        optionalComma = false;
        indent--;

        if (indent < 0)
            throw new InvalidOperationException("Closing too many blocks");

        if (lineHasContent)
            NewLine();
        else
        {
            lineBuilder.Clear();
        }

        InternalAppend("}");
        NewLine();

        return this;
    }
}
