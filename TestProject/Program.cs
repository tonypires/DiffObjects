using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    class Program
    {
        static void Main(string[] args)
        {
            var beforeObj = new MyClass
            {
                Id = 1,
                Name = "Tony Pires",
                Email = "ampires9@gmail.com"
            };

            var afterObj = new MyClass
            {
                Id = 1,
                Name = "Tony",
                Email = "ampires9@gmail.com"
            };

            Console.WriteLine(beforeObj.ToString());
            Console.WriteLine(afterObj.ToString());

            if (Attribute.IsDefined(beforeObj.GetType(), typeof(TrackDifferential)))
            {
                Perform(beforeObj, afterObj);
            }

            Console.ReadKey();
        }

        private static void Perform<T>(T before, T after)
        {
            var beforeProps = GetDiffProps(before);
            var afterProps = GetDiffProps(after);

            var tupleDiff = beforeProps.Count >= afterProps.Count 
                ? Tuple.Create(beforeProps, afterProps) 
                : Tuple.Create(afterProps, beforeProps);

            var differences = new List<Tuple<PropertyInfo, object, string>>();

            var left = tupleDiff.Item1;
            var right = tupleDiff.Item2;

            foreach (var item in left)
            {
                var key = item.Key;
                var val = item.Value;

                if (right.ContainsKey(key))
                {
                    var rightItem = right[key];
                    if (!(rightItem.Item3 == val.Item3))
                    {
                        differences.Add(rightItem);
                    }
                }
                else
                {
                    Console.WriteLine($"The following property could not be found when performing a diff:  {key}");
                }
            }

            if (differences.Count == 0)
            {
                Console.WriteLine("No differences found.");
            }
            else if (differences.Count > 0)
            {
                Console.WriteLine("Differences found for the following property names:");
                foreach (var item in differences)
                {
                    Console.WriteLine(item.Item1.Name);
                }
            }
        }

        private static Dictionary<string, Tuple<PropertyInfo, object, string>> GetDiffProps<T>(T obj)
        {
            return obj.GetType()
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(TrackDiff)))
                .OrderBy(x => x.Name)
                .ToDictionary(x => x.Name, x => GenTuple(obj, x));
        }


        /// <summary>
        /// Returns a tuple containing the PropertyInfo itself (Item1), value of the property value (Item2), and the serialized value (Item3).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="prop">The property.</param>
        /// <returns></returns>
        private static Tuple<PropertyInfo, object, string> GenTuple<T>(T obj, PropertyInfo prop)
        {
            var rawValue = prop.GetValue(obj);
            return Tuple.Create(prop, rawValue, JsonConvert.SerializeObject(rawValue));
        }

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }

    [TrackDifferential]
    class MyClass
    {
        public int Id { get; set; }

        [TrackDiff]
        public string Name { get; set; }

        [TrackDiff]
        public string Email { get; set; }

        public override string ToString()
        {
            return $"{Id}:{Name}:{Email}";
        }
    }

    class TrackDifferential : Attribute
    {
        // Attribute for marking properties to be used in the process of running a differential
        // to identify whether any changes have been made to the object compared to its prior state
    }

    class TrackDiff : Attribute
    {

    }
}
