using System;
using System.Linq;

namespace FingerprintGenerator
{
    class Program
    {
        private static readonly char[] Separators = { ' ', '-', '\'', '.', ',', '?' };
    
        static void Main(string[] args)
        {
            if (args.Length == 0)
                Print(new Random().Next());
            
            foreach (var arg in args)
            {
                Print(arg);
            }
        }

        private static void Print(string seed)
        {
            Console.WriteLine($"Seed: {seed}");
            var print = new Fingerprint(ToInt(seed));
            foreach(var line in print.PatternMap())
                Console.WriteLine(line);
        } //

        private static void Print(int seed) => Print(seed.ToString());
        
        // From base36 to base10
        private static int ToInt(string input)
        {
            string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";
            input = Sanitize(input);
            var reversed = input.ToLower().Reverse();
            int result = 0;
            int pos = 0;
            foreach (char c in reversed)
            {
                result += CharList.IndexOf(c) * (int) Math.Pow(36, pos);
                pos++;
            }
            return result;
        }
        
        //get the input ready to be converted to a base36 number
        public static string Sanitize(string input)
        {
            string[] afterSplit = input.Split(Separators);
            string output = "";
            foreach (string s in afterSplit)
                output += s;
            
            return output;
        }
    }
}