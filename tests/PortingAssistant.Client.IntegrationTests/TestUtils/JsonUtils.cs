using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace PortingAssistant.Client.IntegrationTests.TestUtils
{
    public static class JsonUtils
    {
        public static bool AreTwoJsonFilesEqual(
            string filePath1, string filePath2, string[] propertiesToBeRemoved)
        {
            dynamic jObject1 = JsonConvert.DeserializeObject(
                File.ReadAllText(filePath1));
            dynamic jObject2 = JsonConvert.DeserializeObject(
                File.ReadAllText(filePath2));

            if (!IsNullOrEmpty(propertiesToBeRemoved))
            {
                RemoveProperties(jObject1, propertiesToBeRemoved);
                RemoveProperties(jObject2, propertiesToBeRemoved);
            }

            JObject patch = FindJsonDiff(jObject1, jObject2);
            Console.WriteLine("---------DIFF-----------");
            Console.WriteLine(patch.ToString());
            return patch.Count == 0;
        }

        public static JObject FindJsonDiff(this JToken Current, JToken Model)
        {
            var diff = new JObject();
            if (JToken.DeepEquals(Current, Model)) return diff;

            switch (Current.Type)
            {
                case JTokenType.Object:
                    {
                        var current = Current as JObject;
                        var model = Model as JObject;
                        var addedKeys = current.Properties().Select(c => c.Name).Except(model.Properties().Select(c => c.Name));
                        var removedKeys = model.Properties().Select(c => c.Name).Except(current.Properties().Select(c => c.Name));
                        var unchangedKeys = current.Properties().Where(c => JToken.DeepEquals(c.Value, Model[c.Name])).Select(c => c.Name);
                        foreach (var k in addedKeys)
                        {
                            diff[k] = new JObject
                            {
                                ["+"] = Current[k]
                            };
                        }
                        foreach (var k in removedKeys)
                        {
                            diff[k] = new JObject
                            {
                                ["-"] = Model[k]
                            };
                        }
                        var potentiallyModifiedKeys = current.Properties().Select(c => c.Name).Except(addedKeys).Except(unchangedKeys);
                        foreach (var k in potentiallyModifiedKeys)
                        {
                            var foundDiff = FindJsonDiff(current[k], model[k]);
                            if (foundDiff.HasValues) diff[k] = foundDiff;
                        }
                    }
                    break;
                case JTokenType.Array:
                    {
                        var current = Current as JArray;
                        var model = Model as JArray;
                        var plus = new JArray(current.Except(model, new JTokenEqualityComparer()));
                        var minus = new JArray(model.Except(current, new JTokenEqualityComparer()));
                        if (plus.HasValues) diff["+"] = plus;
                        if (minus.HasValues) diff["-"] = minus;
                    }
                    break;
                default:
                    diff["+"] = Current;
                    diff["-"] = Model;
                    break;
            }

            return diff;
        }

        public static bool IsNullOrEmpty(string[] myStringArray)
        {
            return myStringArray == null || myStringArray.Length < 1;
        }

        private static void RemoveProperties(JToken token, string[] propertiesToBeRemoved)
        {
            JContainer container = token as JContainer;
            if (container == null) return;

            List<JToken> removeList = new List<JToken>();
            foreach (JToken el in container.Children())
            {
                JProperty p = el as JProperty;
                if (p != null && Array.IndexOf(propertiesToBeRemoved, p.Name) >= 0)
                {
                    removeList.Add(el);
                }
                RemoveProperties(el, propertiesToBeRemoved);
            }

            foreach (JToken el in removeList)
            {
                el.Remove();
            }
        }
    }
}
