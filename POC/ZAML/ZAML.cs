using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace System.Text.Zaml
{
    public class OffsideParser
    {
        internal sealed class LineReader
        {
            private readonly string[] lines; private int at = -1;
            internal LineReader(string[] lines) => this.lines = lines;
            internal string Next() { int tail; while (++at < lines.Length && (string.Equals(lines[at].Trim(), string.Empty) || string.Equals((lines[at] = (tail = lines[at].IndexOf("//")) >= 0 ? lines[at].Substring(0, tail).TrimEnd() : lines[at]).Trim(), string.Empty))) ; return at < lines.Length ? lines[at] : null; }
            internal int LineNo => at + 1;
        }
        private readonly int indent;
        private static readonly Regex Hosted = new Regex("\\(\\#[^\\:]+\\:.*?\\#\\)", RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex Literals = new Regex("\"(\\\\\"|[^\"])*\"", RegexOptions.Compiled);
        private static KeyValuePair<string, object> NewKey(string name) => new KeyValuePair<string, object>(name, null);
        private static object EmptyArray() => new object[0];
        private static object EmptyObject() => new Dictionary<string, object>(); // \(\#[^\:\s]+\:(.|\n)*\#\)
        private static (object, string) DoParse(LineReader reader, Func<string, string, object> toHostValue, int indent, int at = 0, string nextLine = null)
        {
            static int IndentOf(string line) { int count = 0, c = -1; while (++c < line.Length && char.IsWhiteSpace(line[c])) count++; return count; }
            var parse = new List<object>(); var hash = false; string line;
            while (true)
            {
                if ((line = nextLine ?? reader.Next()) == null) return (parse.ToArray(), string.Empty);
                var dent = IndentOf(line);
                nextLine = null;
                if ((dent % indent) != 0) throw new InvalidOperationException($"Invalid indentation ({dent}) at line {reader.LineNo}");
                if (dent == at)
                {
                    var value = line.Substring(dent); int colon;
                    if (value.StartsWith("(#"))
                    {
                        int c, e;
                        if (toHostValue != null && (c = value.IndexOf(':')) > 2 && (e = value.IndexOf("#)")) > c)
                        {
                            string type = value.Substring(2, c - 2).Trim(), data = value.Substring(c + 1, e - c - 1).Trim();
                            var hosted = toHostValue(type, data.Substring(1, data.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\"));
                            if (!ReferenceEquals(hosted, typeof(void))) parse.Add(hosted); else throw new InvalidOperationException($"Unsupported value '{value}' at line {reader.LineNo}");
                        }
                        else throw new InvalidOperationException($"Malformed value '{value}' at line {reader.LineNo}");
                    }
                    else if ((colon = value.IndexOf(':')) > 0)
                    {
                        string name;
                        if (value.IndexOf("(#") > colon)
                        {
                            int c, e;
                            name = value.Substring(0, colon).Trim(); value = value.Substring(colon + 1).Trim();
                            if (toHostValue != null && (c = value.IndexOf(':')) > 2 && (e = value.IndexOf("#)")) > c)
                            {
                                string type = value.Substring(2, c - 2).Trim(), data = value.Substring(c + 1, e - c - 1).Trim();
                                var hosted = toHostValue(type, data.Substring(1, data.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\"));
                                parse.Add(NewKey(name));
                                if (!ReferenceEquals(hosted, typeof(void))) parse.Add(hosted); else throw new InvalidOperationException($"Unsupported value '{value}' at line {reader.LineNo}");
                            }
                            else throw new InvalidOperationException($"Malformed value '{value}' at line {reader.LineNo}");
                        }
                        else if ((hash = colon < value.LastIndexOf('#')) || colon < value.LastIndexOf('@'))
                        {
                            name = value.Substring(0, colon);
                            name = name.StartsWith('"') && name.EndsWith('"') ? name.Substring(1, name.Length - 2) : name;
                            parse.Add(NewKey(name));
                        }
                        else
                        {
                            name = value.Substring(0, colon);
                            value = value.Substring(colon + 1).Trim();
                            if (value.Length > 0)
                            {
                                name = name.StartsWith('"') && name.EndsWith('"') ? name.Substring(1, name.Length - 2) : name;
                                parse.Add(NewKey(name));
                                if (value == "#")
                                {
                                    // (Non-empty object literal)
                                }
                                else if (value == "[]")
                                {
                                    parse.Add(EmptyArray());
                                }
                                else if (value == "{}")
                                {
                                    parse.Add(EmptyObject());
                                }
                                else if (value == "false")
                                {
                                    parse.Add(false);
                                }
                                else if (value == "true")
                                {
                                    parse.Add(true);
                                }
                                else if (value == "null")
                                {
                                    parse.Add(null);
                                }
                                else
                                {
                                    if (int.TryParse(value, out var i32)) parse.Add(i32);
                                    else if (long.TryParse(value, out var i64)) parse.Add(i64);
                                    else if (decimal.TryParse(value, out var d)) parse.Add(d);
                                    else if (value.StartsWith('"') && value.EndsWith('"'))
                                    {
                                        parse.Add(value.Substring(1, value.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\""));
                                    }
                                    else
                                    {
                                        parse.Add(value);
                                    }
                                }
                            }
                            else throw new InvalidOperationException($"Malformed value '{value}' at line {reader.LineNo}");
                        }
                    }
                    else if (value == "#")
                    {
                        parse.Add(NewKey(string.Empty));
                    }
                    else if (value == "@")
                    {
                        // (Non-empty array literal)
                    }
                    else if (value == "[]")
                    {
                        parse.Add(EmptyArray());
                    }
                    else if (value == "{}")
                    {
                        parse.Add(EmptyObject());
                    }
                    else if (value == "false")
                    {
                        parse.Add(false);
                    }
                    else if (value == "true")
                    {
                        parse.Add(true);
                    }
                    else if (value == "null")
                    {
                        parse.Add(null);
                    }
                    else
                    {
                        if (int.TryParse(value, out var i32)) parse.Add(i32);
                        else if (long.TryParse(value, out var i64)) parse.Add(i64);
                        else if (decimal.TryParse(value, out var d)) parse.Add(d);
                        else if (value.StartsWith('"') && value.EndsWith('"'))
                        {
                            parse.Add(value.Substring(1, value.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\""));
                        }
                        else
                        {
                            parse.Add(value);
                        }
                    }
                }
                else if (dent > at)
                {
                    if (dent == at + indent)
                    {
                        var child = DoParse(reader, toHostValue, indent, dent, line);
                        nextLine = child.Item2;
                        if (hash)
                        {
                            parse.Add(new object[2] { NewKey(null), child.Item1 });
                        }
                        else
                        {
                            parse.Add(child.Item1);
                        }
                        hash = false;
                    }
                    else throw new InvalidOperationException($"Invalid indentation ({dent}) at line {reader.LineNo}");
                }
                else
                {
                    return (parse.ToArray(), line);
                }
            }
        }
        public OffsideParser(int indent = 0) => this.indent = indent > 1 ? indent : 2;
        public object Parse(string input)
        {
            static string AsHosted(Match match)
            {
                var c = match.Value.IndexOf(':');
                var e = match.Value.IndexOf("#)");
                var type = match.Value.Substring(2, c - 2).Trim();
                var data = match.Value.Substring(c + 1, e - c - 1).Trim();
                return $"(# {type} : \"{data.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")}\" #)";
            }
            static string AsLiteral(Match match) => match.Value.Replace("\n", "\\n");
            static bool IsValid(Func<object, bool> isHostValue, object o) =>
                o == null || (o is Dictionary<string, object> d && d.Values.All(x => IsValid(isHostValue, x))) || (o is object[] a && a.All(x => IsValid(isHostValue, x))) ||
                o is string || o is decimal || o is long || o is int || o is bool || isHostValue != null && isHostValue(o);
            static object Normalize(object o)
            {
                if (o is object[] a && a.Length > 0)
                {
                    if (a.Length > 1 && a[0] is KeyValuePair<string, object> k && k.Key == null && a[1] is object[] m)
                    {
                        var d = new Dictionary<string, object>();
                        for (var i = 0; i < m.Length; i += 2)
                        {
                            d.Add(((KeyValuePair<string, object>)m[i]).Key, Normalize(m[i + 1]));
                        }
                        return d;
                    }
                    else
                    {
                        var r = new List<object>();
                        var i = 0;
                        while (i < a.Length)
                        {
                            if (a[i] is KeyValuePair<string, object>)
                            {
                                if (i < a.Length - 1 && a[i + 1] is object[] c)
                                {
                                    var d = new Dictionary<string, object>();
                                    for (var j = 0; j < c.Length; j += 2)
                                    {
                                        d.Add(((KeyValuePair<string, object>)c[j]).Key, Normalize(c[j + 1]));
                                    }
                                    r.Add(d);
                                    i += 2;
                                }
                                else
                                {
                                    throw new InvalidOperationException("Invalid input parse");
                                }
                            }
                            else
                            {
                                r.Add(Normalize(a[i++]));
                            }
                        }
                        return r.ToArray();
                    }
                }
                return o;
            }
            var reader = new LineReader(Literals.Replace(Hosted.Replace(input.ReplaceLineEndings("\n"), AsHosted), AsLiteral).Split('\n'));
            var parse = DoParse(reader, ToHostValue ?? ((type, data) => typeof(void)), indent).Item1; object[] array;
            if ((array = parse as object[]) != null && array.Length > 0 && ReferenceEquals(array[array.Length - 1], string.Empty)) Array.Resize(ref array, array.Length - 1);
            return IsValid(IsHostValue, parse = Normalize(array)) ? parse : throw new InvalidOperationException("Invalid object layout");
        }
        public Func<string, string, object> ToHostValue { get; set; }
        public Func<object, bool> IsHostValue { get; set; }
    }
}