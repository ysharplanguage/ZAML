using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.ZAML;
namespace ZAMLPOC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var expected = @"[
  {
    ""xml"": [
      ""very rigid"",
      ""hurts the eyes...""
    ],
    ""json"": [
      ""rigid"",
      ""better for data interchange""
    ],
    ""a date"": ""2025-02-21"",
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
      ""even better for configuration...\n... and,\n    well... \""yeah!\""...\n other things"",
      ""smarter arrays (of arrays of arrays...)""
    ],
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
          ""truth"",
          3.1416,
          4,
          5,
          -65537,
          7,
          ""eight"",
          9
        ],
        {
          ""id"": ""I'm an object, somewhat buried ;^)"",
          ""when"": ""1970-03-01""
        },
        ""e"",
        {}
      ],
      "" foo...\n    ... and bar..."",
      ""$"",
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
            var zaml = new ZAMLParser();
            var from = File.ReadAllText("ZAMLPOC-Test-Me.txt"/*args[0]*/);
            var into = zaml.Parse(from);
            var json = JsonSerializer.Serialize(into, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            System.Diagnostics.Debug.Assert(json == expected);
            Console.WriteLine(from);
            Console.WriteLine();
            Console.WriteLine(json);
            Console.WriteLine("The end");
            Console.ReadKey(true);
        }
    }
}