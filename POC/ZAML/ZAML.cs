using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
namespace System.Text.ZAML
{
    public class ZAMLParser
    {
        internal sealed class LineReader
        {
            private readonly string[] lines;
            private int at = -1;
            internal LineReader(string[] lines) => this.lines = lines;
            internal string Next() { int tail; while (++at < lines.Length && (string.Equals(lines[at].Trim(), string.Empty) || string.Equals((lines[at] = (tail = lines[at].IndexOf("///")) >= 0 ? lines[at].Substring(0, tail).TrimEnd() : lines[at]).Trim(), string.Empty))) ; return at < lines.Length ? lines[at] : null; }
        }
        private int indent = 0;
        private static readonly Regex Spaces = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex Special = new Regex("(\\\\|\\\"|\\@|\\#|\\[|\\]|\\{|\\}|\\(|\\))", RegexOptions.Compiled);
        private static readonly Regex Literals = new Regex("\"(\\\\\"|[^\"])*\"", RegexOptions.Compiled);
        private static KeyValuePair<string, object> NewKey(string name) => new KeyValuePair<string, object>(name, null);
        private static object EmptyArray() => new object[0];
        private static object EmptyObject() => new Dictionary<string, object>();
        private static (object, string) DoParse(LineReader reader, int indent, int at = 0, string nextLine = null)
        {
            static int IndentOf(string line) { int count = 0, c = -1; while (++c < line.Length && char.IsWhiteSpace(line[c])) count++; return count; }
            static string ToKey(string name) => name.StartsWith('"') && name.EndsWith('"') ? name.Substring(1, name.Length - 2) : name;
            static List<object> InlinedList(string value)
            {
                var split = Spaces.Replace(value, " ").Split(' ');
                var list = new List<object>();
                for (var i = 0; i < split.Length; i++)
                {
                    var item = split[i].Trim();
                    if (i == 0 && item == "@") continue;
                    else if (item == "[]") list.Add(EmptyArray());
                    else if (item == "{}") list.Add(EmptyObject());
                    else if (item == "false") list.Add(false);
                    else if (item == "true") list.Add(true);
                    else if (item == "null") list.Add(null);
                    else if (int.TryParse(item, out var si32)) list.Add(si32);
                    else if (long.TryParse(item, out var si64)) list.Add(si64);
                    else if (decimal.TryParse(item, out var sd)) list.Add(sd);
                    else if (!Special.Match(item.Substring(0, 1)).Success) list.Add(item);
                    else throw new InvalidOperationException($"Malformed value '{item}'");
                }
                return list;
            }
            var parse = new List<object>();
            var isMap = false;
            string line;
            while (true)
            {
                int dent;
                if ((line = nextLine ?? reader.Next()) == null) return (parse, string.Empty);
                nextLine = null;
                if (((dent = IndentOf(line)) == 1 && indent == 0) || (indent > 0 && (dent % indent) != 0)) throw new InvalidOperationException($"Invalid indentation ({dent})");
                if (indent == 0 && dent > 0) indent = dent;
                if (dent == at)
                {
                    var value = line.Substring(dent);
                    int colon;
                    if ((colon = value.IndexOf(':')) >= 0 && (!value.StartsWith('(') || !value.TrimEnd().EndsWith(')')))
                    {
                        string tail = value.Substring(colon + 1), name;
                        int a, p, q;
                        if ((isMap = colon < (p = value.IndexOf('#')) && ((q = tail.IndexOf('"')) < 0 || p < q)) || ((a = value.IndexOf('@')) < 0 && tail.Trim() == string.Empty) || colon < a)
                        {
                            name = value.Substring(0, colon).Trim();
                            value = tail.Trim();
                            parse.Add(NewKey(ToKey(name)));
                            if (value.StartsWith('@'))
                            {
                                if (value.Length > 1 && char.IsWhiteSpace(value[1]) && !char.IsWhiteSpace(value[value.Length - 1])) parse.Add(InlinedList(value));
                                else if (value == "@") { }
                                else throw new InvalidOperationException($"Malformed value '{value}'");
                            }
                        }
                        else
                        {
                            name = value.Substring(0, colon).Trim();
                            value = value.Substring(colon + 1).Trim();
                            if (value.Length > 0)
                            {
                                parse.Add(NewKey(ToKey(name)));
                                if (value == "#") { /* (Non-empty object literal) */ }
                                else if (value == "[]") parse.Add(EmptyArray());
                                else if (value == "{}") parse.Add(EmptyObject());
                                else if (value == "false") parse.Add(false);
                                else if (value == "true") parse.Add(true);
                                else if (value == "null") parse.Add(null);
                                else if (int.TryParse(value, out var i32)) parse.Add(i32);
                                else if (long.TryParse(value, out var i64)) parse.Add(i64);
                                else if (decimal.TryParse(value, out var d)) parse.Add(d);
                                else if (value.StartsWith('"') && value.EndsWith('"')) parse.Add(value.Substring(1, value.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\""));
                                else if (value.StartsWith('(') && value.EndsWith(')')) parse.Add(value);
                                else if (Spaces.Match(value).Success && value.Any(c => !char.IsWhiteSpace(c)) && !Special.Match(value.Substring(0, 1)).Success) parse.Add(InlinedList(value));
                                else if (!Special.Match(value.Substring(0, 1)).Success) parse.Add(value);
                                else throw new InvalidOperationException($"Malformed value '{value}'");
                            }
                            else throw new InvalidOperationException($"Malformed value '{value}'");
                        }
                    }
                    else if (value == "#") parse.Add(NewKey(string.Empty));
                    else if (value.StartsWith('@'))
                    {
                        if (value.Length > 1 && char.IsWhiteSpace(value[1]) && !char.IsWhiteSpace(value[value.Length - 1])) parse.Add(InlinedList(value));
                        else if (value == "@") { }
                        else throw new InvalidOperationException($"Malformed value '{value}'");
                    }
                    else if (value == "[]") parse.Add(EmptyArray());
                    else if (value == "{}") parse.Add(EmptyObject());
                    else if (value == "false") parse.Add(false);
                    else if (value == "true") parse.Add(true);
                    else if (value == "null") parse.Add(null);
                    else if (int.TryParse(value, out var i32)) parse.Add(i32);
                    else if (long.TryParse(value, out var i64)) parse.Add(i64);
                    else if (decimal.TryParse(value, out var d)) parse.Add(d);
                    else if (value.StartsWith('"') && value.EndsWith('"')) parse.Add(value.Substring(1, value.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\""));
                    else if (value.StartsWith('(') && value.EndsWith(')')) parse.Add(value);
                    else if (Spaces.Match(value).Success && value.Any(c => !char.IsWhiteSpace(c)) && !Special.Match(value.Substring(0, 1)).Success) parse.Add(InlinedList(value));
                    else if (value.Length > 0 && !Special.Match(value.Substring(0, 1)).Success) parse.Add(value);
                    else if (value.Length == 0) { }
                    else throw new InvalidOperationException($"Malformed value '{value}'");
                }
                else if (dent > at)
                {
                    if (dent == at + indent)
                    {
                        var child = DoParse(reader, indent, dent, line);
                        nextLine = child.Item2;
                        if (isMap) parse.Add(new List<object> { NewKey(null), child.Item1 }); else parse.Add(child.Item1);
                        isMap = false;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid indentation ({dent})");
                    }
                }
                else
                {
                    return (parse, line);
                }
            }
        }
        public ZAMLParser(int indent = 0) => this.indent = indent > 1 ? indent : 0;
        public object Parse(string input)
        {
            static string AsLiteral(Match match) => match.Value.Replace("\n", "\\n");
            static bool IsValid(object o) =>
                o == null ||
                (o is Dictionary<string, object> d && d.Values.All(x => IsValid(x))) ||
                (o is IEnumerable<object> a && a.All(x => IsValid(x))) ||
                o is string || o is decimal || o is long || o is int || o is bool;
            static object Normalize1(object o)
            {
                if (o is List<object> a && a.Count > 0)
                {
                    if (a.Count > 1 && a[0] is KeyValuePair<string, object> k && k.Key == null && a[1] is List<object> m)
                    {
                        var d = new Dictionary<string, object>();
                        for (var i = 0; i < m.Count; i += 2) d.Add(((KeyValuePair<string, object>)m[i]).Key, Normalize1(m[i + 1]));
                        return d;
                    }
                    else
                    {
                        var r = new List<object>();
                        var i = 0;
                        while (i < a.Count)
                        {
                            if (a[i] is KeyValuePair<string, object>)
                            {
                                if (i < a.Count - 1 && a[i + 1] is List<object> c)
                                {
                                    var d = new Dictionary<string, object>();
                                    for (var j = 0; j < c.Count; j += 2)
                                    {
                                        d.Add(((KeyValuePair<string, object>)c[j]).Key, Normalize1(c[j + 1]));
                                    }
                                    r.Add(d);
                                    i += 2;
                                }
                                else throw new InvalidOperationException("Invalid input parse");
                            }
                            else r.Add(Normalize1(a[i++]));
                        }
                        return r;
                    }
                }
                return o;
            }
            static object Normalize2(object o) =>
                o is Dictionary<string, object> d ? d.ToDictionary(p => p.Key, p => Normalize2(p.Value)) : o is List<object> l ? l.Select(x => Normalize2(x)).ToArray() : o;
            var reader = new LineReader(Literals.Replace(input.ReplaceLineEndings("\n"), AsLiteral).Split('\n'));
            var list = (List<object>)DoParse(reader, indent).Item1;
            object parse;
            if (list.Count > 0 && ReferenceEquals(list[list.Count - 1], string.Empty)) list.RemoveAt(list.Count - 1);
            return IsValid(parse = Normalize2(Normalize1(list))) ? parse : throw new InvalidOperationException("Invalid object layout");
        }
    }
}