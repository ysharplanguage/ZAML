using System;
using System.IO;
using System.Text.Json;
using System.Text.Zaml;
namespace ZAML
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var expected_json = @"[
  {
    ""json"": [
      ""rigid"",
      ""better for data interchange""
    ],
    ""yaml"": [
      ""slim and flexible"",
      ""better for configuration""
    ],
    ""zaml"": [
      ""Zero (Almost!) Markup Language"",
      ""more consistent than YAML"",
      ""even better for configuration...\n... and,\n    well... \u0022yeah!\u0022...\n other things"",
      ""smarter arrays (of arrays of arrays...)"",
      ""2025-02-20T00:00:00""
    ],
    ""nice_ZAML_array_of_array"": [
      ""a"",
      [
        ""c"",
        {
          ""id"": ""I am an object, somewhat buried ;^)"",
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
            var input = File.ReadAllText("ZAML-Test-Me.txt"/*args[0]*/);
            var parser = new OffsideParser(3) {
                ToHostValue =
                    (type, data) =>
                        type == "DateTime" ? DateTime.Parse(data) : typeof(void),
                IsHostValue =
                    value =>
                        value is DateTime
            };
            var parse = parser.Parse(input);
            var json = JsonSerializer.Serialize(parse, new JsonSerializerOptions { WriteIndented = true });
            System.Diagnostics.Debug.Assert(json == expected_json);
            Console.WriteLine(json);
            Console.ReadKey(true);
        }
    }
}