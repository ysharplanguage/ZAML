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
            internal string Next() { int tail; while (++at < lines.Length && (string.Equals(lines[at].Trim(), string.Empty) || string.Equals((lines[at] = (tail = lines[at].IndexOf("//")) >= 0 ? lines[at].Substring(0, tail).TrimEnd() : lines[at]).Trim(), string.Empty))) ; return at < lines.Length ? lines[at] : null; }
        }
        private int indent = 0;
        private static readonly Regex Spaces = new Regex("\\s+", RegexOptions.Compiled);
        private static readonly Regex Hosted = new Regex("\\(\\#[^\\:]+\\:.*?\\#\\)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex Literals = new Regex("\"(\\\\\"|[^\"])*\"", RegexOptions.Compiled);
        private static readonly Regex Interpolation = new("\\{\\#.+?\\#\\}", RegexOptions.Compiled | RegexOptions.Singleline);
        private static KeyValuePair<string, object> NewKey(string name) => new KeyValuePair<string, object>(name, null);
        private static object EmptyArray() => new object[0];
        private static object EmptyObject() => new Dictionary<string, object>();
        private static (object, string) DoParse(LineReader reader, Func<string, string, object> asHostValue, Func<object, string, object> toHostValue, object context, int indent, int at = 0, string nextLine = null)
        {
            string Interpolated(Match match) => toHostValue(context, match.Value.Substring(2, match.Value.Length - 4))?.ToString() ?? string.Empty;
            static int IndentOf(string line) { int count = 0, c = -1; while (++c < line.Length && char.IsWhiteSpace(line[c])) count++; return count; }
            static string ToKey(string name) => name.StartsWith('"') && name.EndsWith('"') ? name.Substring(1, name.Length - 2) : name;
            var parse = new List<object>();
            var hash = false;
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
                    var value = line.Substring(dent); int colon;
                    if (value.StartsWith("(#"))
                    {
                        int c, e;
                        if (asHostValue != null && (c = value.IndexOf(':')) > 2 && (e = value.IndexOf("#)")) > c)
                        {
                            string type = value.Substring(2, c - 2).Trim(), data = value.Substring(c + 1, e - c - 1).Trim();
                            var hosted = asHostValue(type, data.Substring(1, data.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\"));
                            if (!ReferenceEquals(hosted, typeof(void))) parse.Add(hosted); else throw new InvalidOperationException($"Unsupported value '{value}'");
                        }
                        else throw new InvalidOperationException($"Malformed value '{value}'");
                    }
                    else if ((colon = value.IndexOf(':')) >= 0)
                    {
                        string name;
                        if (value.IndexOf("(#") > colon)
                        {
                            int c, e;
                            name = value.Substring(0, colon).Trim();
                            value = value.Substring(colon + 1).Trim();
                            if (asHostValue != null && (c = value.IndexOf(':')) > 2 && (e = value.IndexOf("#)")) > c)
                            {
                                string type = value.Substring(2, c - 2).Trim(), data = value.Substring(c + 1, e - c - 1).Trim();
                                var hosted = asHostValue(type, data.Substring(1, data.Length - 2).Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\"));
                                parse.Add(NewKey(ToKey(name)));
                                if (!ReferenceEquals(hosted, typeof(void))) parse.Add(hosted); else throw new InvalidOperationException($"Unsupported value '{value}'");
                            }
                            else throw new InvalidOperationException($"Malformed value '{value}'");
                        }
                        else if ((hash = colon < value.LastIndexOf('#')) || colon < value.LastIndexOf('@'))
                        {
                            name = value.Substring(0, colon).Trim();
                            parse.Add(NewKey(ToKey(name)));
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
                                else if ((value.StartsWith('"') || value.StartsWith("$\"")) && value.EndsWith('"'))
                                {
                                    var bound = value.StartsWith("$\"") ? 2 : 1;
                                    var text = value.Substring(bound, value.Length - bound - 1).Replace("\\n", "\n").Replace("\\\"", "\"");
                                    parse.Add(bound > 1 ? Interpolation.Replace(text, Interpolated) : text);
                                }
                                else if (Spaces.Match(value).Success && value.IndexOf('"') < 0)
                                {
                                    var split = Spaces.Replace(value, " ").Split(' ');
                                    var list = new List<object>();
                                    for (var i = 0; i < split.Length; i++)
                                    {
                                        var item = split[i].Trim();
                                        if (item == "false") list.Add(false);
                                        else if (item == "true") list.Add(true);
                                        else if (item == "null") list.Add(null);
                                        else if (int.TryParse(item, out var si32)) list.Add(si32);
                                        else if (long.TryParse(item, out var si64)) list.Add(si64);
                                        else if (decimal.TryParse(item, out var sd)) list.Add(sd);
                                        else if (item.Length > 0 && item[0] == '$') list.Add(asHostValue(null, item.Substring(1)));
                                        else if (item.Length > 0 && (item[0] == '_' || char.IsLetter(item[0]))) list.Add(item);
                                        else throw new InvalidOperationException($"Malformed value '{item}'");
                                    }
                                    parse.Add(list);
                                }
                                else if (value.Length > 0 && value[0] == '$') parse.Add(value.Substring(1));
                                else if (value.Length > 0 && (value[0] == '_' || char.IsLetter(value[0]))) parse.Add(value);
                                else throw new InvalidOperationException($"Malformed value '{value}'");
                            }
                            else throw new InvalidOperationException($"Malformed value '{value}'");
                        }
                    }
                    else if (value == "#") parse.Add(NewKey(string.Empty));
                    else if (value == "@") { /* Non-empty array literal) */ }
                    else if (value == "[]") parse.Add(EmptyArray());
                    else if (value == "{}") parse.Add(EmptyObject());
                    else if (value == "false") parse.Add(false);
                    else if (value == "true") parse.Add(true);
                    else if (value == "null") parse.Add(null);
                    else if (int.TryParse(value, out var i32)) parse.Add(i32);
                    else if (long.TryParse(value, out var i64)) parse.Add(i64);
                    else if (decimal.TryParse(value, out var d)) parse.Add(d);
                    else if ((value.StartsWith('"') || value.StartsWith("$\"")) && value.EndsWith('"'))
                    {
                        var bound = value.StartsWith("$\"") ? 2 : 1;
                        var text = value.Substring(bound, value.Length - bound - 1).Replace("\\n", "\n").Replace("\\\"", "\"");
                        parse.Add(bound > 1 ? Interpolation.Replace(text, Interpolated) : text);
                    }
                    else if (Spaces.Match(value).Success && value.IndexOf('"') < 0)
                    {
                        var split = Spaces.Replace(value, " ").Split(' ');
                        var list = new List<object>();
                        for (var i = 0; i < split.Length; i++)
                        {
                            var item = split[i].Trim();
                            if (item == "false") list.Add(false);
                            else if (item == "true") list.Add(true);
                            else if (item == "null") list.Add(null);
                            else if (int.TryParse(item, out var si32)) list.Add(si32);
                            else if (long.TryParse(item, out var si64)) list.Add(si64);
                            else if (decimal.TryParse(item, out var sd)) list.Add(sd);
                            else if (item.Length > 0 && item[0] == '$') list.Add(asHostValue(null, item.Substring(1)));
                            else if (item.Length > 0 && (item[0] == '_' || char.IsLetter(item[0]))) list.Add(item);
                            else throw new InvalidOperationException($"Malformed value '{item}'");
                        }
                        parse.Add(list);
                    }
                    else if (value.Length > 0 && value[0] == '$') parse.Add(value.Substring(1));
                    else if (value.Length > 0 && (value[0] == '_' || char.IsLetter(value[0]))) parse.Add(value);
                    else if (value == string.Empty) { }
                    else throw new InvalidOperationException($"Malformed value '{value}'");
                }
                else if (dent > at)
                {
                    if (dent == at + indent)
                    {
                        var child = DoParse(reader, asHostValue, toHostValue, context, indent, dent, line);
                        nextLine = child.Item2;
                        if (hash) parse.Add(new List<object> { NewKey(null), child.Item1 }); else parse.Add(child.Item1);
                        hash = false;
                    }
                    else throw new InvalidOperationException($"Invalid indentation ({dent})");
                }
                else
                {
                    return (parse, line);
                }
            }
        }
        public ZAMLParser(int indent = 0) => this.indent = indent > 1 ? indent : 0;
        public object Parse(string input) => Parse(null, input);
        public object Parse(object context, string input)
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
                o == null ||
                (o is Dictionary<string, object> d && d.Values.All(x => IsValid(isHostValue, x))) ||
                (o is IEnumerable<object> a && a.All(x => IsValid(isHostValue, x))) ||
                o is string || o is decimal || o is long || o is int || o is bool ||
                isHostValue != null && isHostValue(o);
            static object Normalize(object o)
            {
                if (o is List<object> a && a.Count > 0)
                {
                    if (a.Count > 1 && a[0] is KeyValuePair<string, object> k && k.Key == null && a[1] is List<object> m)
                    {
                        var d = new Dictionary<string, object>();
                        for (var i = 0; i < m.Count; i += 2)
                        {
                            d.Add(((KeyValuePair<string, object>)m[i]).Key, Normalize(m[i + 1]));
                        }
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
                        return r;
                    }
                }
                return o;
            }
            var reader = new LineReader(Literals.Replace(Hosted.Replace(input.ReplaceLineEndings("\n"), AsHosted), AsLiteral).Split('\n'));
            var parse = DoParse(reader, AsHostValue ?? ((type, data) => typeof(void)), ToHostValue ?? ((context, expr) => expr), context, indent).Item1;
            List<object> list;
            if ((list = parse as List<object>) != null && list.Count > 0 && ReferenceEquals(list[list.Count - 1], string.Empty)) list.RemoveAt(list.Count - 1);
            return IsValid(IsHostValue, parse = Normalize(list)) ? parse : throw new InvalidOperationException("Invalid object layout");
        }
        public Func<string, string, object> AsHostValue { get; set; }
        public Func<object, string, object> ToHostValue { get; set; }
        public Func<object, bool> IsHostValue { get; set; }
    }
}