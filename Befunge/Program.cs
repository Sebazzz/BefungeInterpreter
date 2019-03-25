using System;
using System.Diagnostics;
using System.IO;

namespace Befunge
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string program = TryReadProgram();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Program: ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(program);
            Console.WriteLine("------------------------------------------");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();

            var sw = new Stopwatch();
            sw.Start();

            var interpreter = new BefungeInterpreter();
            foreach (var output in interpreter.InterpretStepByStep(program)) Console.Write(output);

            sw.Stop();

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("------------------------------------------");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine($"{interpreter.InstructionCount} instructions executed in {sw.Elapsed}");

            Console.ReadKey();
        }

        private static string TryReadProgram()
        {
            try
            {
                return File.ReadAllText("Program.txt");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(ex.HResult);
            }

            return null;
        }
    }
}