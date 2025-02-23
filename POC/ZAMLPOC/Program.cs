using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.ZAML;
using System.Xml.Linq;
namespace ZAMLPOC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var expected_json = @"[
  {
    ""xml"": [
      ""very rigid"",
      ""hurts the eyes..."",
      ""<doc id=\""101\"">\r\n                  Hello, \""world\""!\r\n                  back \\ slash\r\n              </doc>"",
      {
        ""smallest_xml"": ""<x />""
      }
    ],
    ""json"": [
      ""rigid"",
      ""better for data interchange""
    ],
    ""a date"": ""2025-02-21T00:00:00"",
    ""digits"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""$digit_names"": [
      ""zero"",
      ""one"",
      ""two"",
      ""three"",
      ""four"",
      ""five"",
      ""six"",
      ""seven"",
      ""eight"",
      ""nine""
    ],
    ""yaml"": [
      ""slim and flexible"",
      ""better for configuration""
    ],
    ""zaml"": [
      ""Zero (Almost!) Markup Language"",
      ""more consistent than YAML"",
      ""even better for configuration...\n... and,\n    well... \""yeah!\""...\n other things (PI^2 = 3.141593 * 3.141593... {but what else?})"",
      ""smarter arrays (of arrays of arrays...)"",
      ""2025-02-20T00:00:00""
    ],
    ""empty"": """",
    ""ten"": ""10"",
    ""nice ZAML array of arrays"": [
      ""a"",
      [
        ""c"",
        [
          0,
          null,
          136129581883,
          false,
          2,
          ""true"",
          3.1416,
          4,
          5,
          -65537,
          7,
          ""8"",
          9
        ],
        {
          ""id"": ""I'm an object, somewhat buried ;^)"",
          ""when"": ""1970-03-01T00:00:00""
        },
        ""e"",
        {}
      ],
      "" foo...\n    ... and bar..."",
      """",
      [],
      ""g e e""
    ],
    ""array_of_objects"": [
      {
        ""null_value"": null,
        """": []
      },
      {
        ""boolean"": true
      },
      {
        ""integer"": -65537
      },
      {
        ""long"": 136129581883
      },
      {
        ""decimal"": 3.1416
      }
    ]
  }
]";
            static object XDocumentToString(object o)
            {
                if (o is Dictionary<string, object> d) return d.ToDictionary(p => p.Key, p => XDocumentToString(p.Value));
                else if (o is List<object> a) return a.Select(XDocumentToString).ToList();
                else if (o is XDocument x) return x.Root.ToString();
                else return o;
            }
            var input = File.ReadAllText("ZAMLPOC-Test-Me.txt"/*args[0]*/);
            var parser = new ZAMLParser()
            {
                AsHostValue = (type, data) =>
                    type == null ? data : // (When prefixed by '$')
                    type == "DateTime" ? DateTime.Parse(data) : type == "XDocument" ? XDocument.Parse(data) :
                    typeof(void),
                ToHostValue = (context, expr) =>
                    ((Dictionary<string, object>)context).TryGetValue(expr.Trim(), out var found) ? found : expr,
                IsHostValue = value =>
                    value is DateTime || value is XDocument
            };
            var context = new Dictionary<string, object>
            {
                ["PI"] = 3.141593m
            };
            var parse = parser.Parse(context, input);
            parse = XDocumentToString(parse);
            var json = JsonSerializer.Serialize(parse, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            System.Diagnostics.Debug.Assert(json == expected_json);
            Console.WriteLine(input); Console.WriteLine();
            Console.WriteLine(json); Console.WriteLine("The end");
            Console.ReadKey(true);
        }
    }
}