using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Zaml;
using System.Xml.Linq;
namespace ZAML
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
    ""digit_names"": [
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
      ""even better for configuration...\n... and,\n    well... \""yeah!\""...\n other things"",
      ""smarter arrays (of arrays of arrays...)"",
      ""2025-02-20T00:00:00""
    ],
    ""nice_ZAML_array_of_array"": [
      ""a"",
      [
        ""c"",
        [
          0,
          null,
          136129581883,
          false,
          2,
          true,
          3.1416,
          [],
          4,
          {},
          5,
          -65537,
          7,
          8,
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
      ""f"",
      [],
      ""g e e""
    ],
    ""array"": [
      {
        ""null_value"": null,
        ""empty"": []
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
                else if (o is object[] a) return a.Select(XDocumentToString).ToArray();
                else if (o is XDocument x) return x.Root.ToString();
                else return o;
            }
            var input = File.ReadAllText("ZAML-Test-Me.txt"/*args[0]*/);
            var parser = new OffsideParser(3) { // TODO: add indent auto detect
                ToHostValue =
                    (type, data) =>
                        type == "DateTime" ? DateTime.Parse(data) : type == "XDocument" ? XDocument.Parse(data) : typeof(void),
                IsHostValue =
                    value => value is DateTime || value is XDocument
            };
            var parse = XDocumentToString(parser.Parse(input));
            var json = JsonSerializer.Serialize(parse, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            System.Diagnostics.Debug.Assert(json == expected_json);
            Console.WriteLine(json);
            Console.WriteLine("The end");
            Console.ReadKey(true);
        }
    }
}