using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.ZAML;
namespace ZAMLTests
{
    [TestClass]
    public class ZAMLParserTests
    {
        [TestMethod]
        public void ZAMLParser_Can_Auto_Detect_Indentation()
        {
            // Arrange
            var parser = new ZAMLParser();
            var input3 = @"
// (Indentation = 3)

// 3 ""objects"" (so to speak)

#
   id: 1
   name: one

#
   id: 2
   name: two

#
   id: 3
   name: three

// (The end)
";

            // Act
            var parsed = parser.Parse(input3);

            // Assert
            Assert.IsNotNull(parsed);
            Assert.IsInstanceOfType(parsed, typeof(List<object>));
            var list = (List<object>)parsed;
            Assert.AreEqual(3, list.Count);
            Assert.IsTrue(list.All(item => item is Dictionary<string, object>));
            var items = list.Select(item => (Dictionary<string, object>)item).ToArray();
            Assert.AreEqual(1, items[0]["id"]);
            Assert.AreEqual("one", items[0]["name"]);
            Assert.AreEqual(2, items[1]["id"]);
            Assert.AreEqual("two", items[1]["name"]);
            Assert.AreEqual(3, items[2]["id"]);
            Assert.AreEqual("three", items[2]["name"]);
        }

        [TestMethod]
        public void ZAMLParser_Can_Parse_With_Ensuring_A_List_As_Root_Node()
        {
            // Arrange
            var parser = new ZAMLParser();
            var input = @"
// (No indentation needed to be inferred)

1

""200""

3.1416 // (Parsed as a decimal)

-4000

5000000000000000000 // (Parsed as a long (Int64))

// (The end)
";

            // Act
            var parsed = parser.Parse(input);

            Assert.IsNotNull(parsed);
            Assert.IsInstanceOfType(parsed, typeof(List<object>));
            var list = (List<object>)parsed;
            Assert.IsTrue(new object[] { 1, "200", 3.1416m, -4000, 5_000_000_000_000_000_000 }.SequenceEqual(list));
        }

        [TestMethod]
        public void ZAMLParser_Can_Parse_Explicit_And_Implicit_Lists()
        {
            // Arrange
            var parser = new ZAMLParser();
            var input3 = @"
// (Indentation = 3)

// 2 lists, with the second containing implicit sub-lists at various levels

@
   1
   ""two""
   three

@
   item1
   @  // (Explicit sub-list)
      item21
      item22
      item23
      
   item3

   // (Implicit sub-list)
      item41

      #             // An ""object"", sibling of item41 and item43
         id: item42
         name: ""4.2""

// (More implicit sub-lists below)

      item43

         item51

         item52

            item521

            item522
            item523

// (The end)
";
            var expected_json = @"[
  [
    1,
    ""two"",
    ""three""
  ],
  [
    ""item1"",
    [
      ""item21"",
      ""item22"",
      ""item23""
    ],
    ""item3"",
    [
      ""item41"",
      {
        ""id"": ""item42"",
        ""name"": ""4.2""
      },
      ""item43"",
      [
        ""item51"",
        ""item52"",
        [
          ""item521"",
          ""item522"",
          ""item523""
        ]
      ]
    ]
  ]
]";

            // Act
            var parsed = parser.Parse(input3);
            var equivalent_json = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });

            // Assert
            Assert.AreEqual(expected_json, equivalent_json);
        }

        [TestMethod]
        public void ZAMLParser_Can_Parse_Two_Dimensional_Matrices()
        {
            // Arrange
            var parser = new ZAMLParser();
            var input4 = @"
// (Indentation = 4)

// A two-dimensional matrix, which makes profit from implicit (and inlined) sub-lists
@
    1   0   0   0   0
    0   1   0   0   0
    0   0   1   0   0
    0   0   0   1   0
    0   0   0   0   1

// (The end)
";
            var expected_json = @"[
  [
    [
      1,
      0,
      0,
      0,
      0
    ],
    [
      0,
      1,
      0,
      0,
      0
    ],
    [
      0,
      0,
      1,
      0,
      0
    ],
    [
      0,
      0,
      0,
      1,
      0
    ],
    [
      0,
      0,
      0,
      0,
      1
    ]
  ]
]";

            // Act
            var parsed = parser.Parse(input4);
            var equivalent_json = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });

            // Assert
            Assert.AreEqual(expected_json, equivalent_json);
        }
    }
}