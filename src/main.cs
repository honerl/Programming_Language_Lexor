using System;
using System.Collections.Generic;
using System.IO;
using LexorInterpreter.Parser;
using LexorInterpreter.Interpreter;
using LexorInterpreter.Shared;
using LexorInterpreter.Lexer;

namespace LexorInterpreter;
class Program
    {
        static void Main(string[] args)
        {
            //  File mode: dotnet run -- myfile.lexor 
            if (args.Length > 0)
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine("Error: File '{0}' not found.", args[0]);
                    return;
                }
                RunSource(File.ReadAllText(args[0]));
                return;
            }

            //  Interactive REPL mode
            RunREPL();
        }
        

        static void RunREPL()
        {
            Console.WriteLine("=============================================");
            Console.WriteLine("        LEXOR Interpreter v1.0");
            Console.WriteLine("=============================================");
            Console.WriteLine("Write your LEXOR program line by line.");
            Console.WriteLine("Commands:");
            Console.WriteLine("  RUN   - execute the program");
            Console.WriteLine("  CLEAR - clear and start over");
            Console.WriteLine("  EXIT  - quit the interpreter");
            Console.WriteLine("=============================================");
            Console.WriteLine();

            List<string> lines = new List<string>();

            while (true)
            {
                Console.Write("LEXOR> ");
                string input = Console.ReadLine();

                // Handle null
                if (input == null) break;

                string trimmed = input.Trim();

                // COMMANDS *TO BE UPDATED PA
                if (trimmed == "EXIT")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (trimmed == "CLEAR")
                {
                    lines.Clear();
                    Console.WriteLine("Program cleared.\n");
                    continue;
                }

                if (trimmed == "LIST")
                {
                    if (lines.Count == 0)
                    {
                        Console.WriteLine("(empty)\n");
                    }
                    else
                    {
                        Console.WriteLine("--- Current Program ---");
                        for (int i = 0; i < lines.Count; i++)
                            Console.WriteLine("{0,3}: {1}", i + 1, lines[i]);
                        Console.WriteLine("-----------------------\n");
                    }
                    continue;
                }

                if (trimmed == "RUN")
                {
                    if (lines.Count == 0)
                    {
                        Console.WriteLine("Nothing to run. Type your program first.\n");
                        continue;
                    }

                    string source = string.Join("\n", lines);
                    Console.WriteLine("\n--- Output ---");
                    RunSource(source);
                    Console.WriteLine("--------------\n");
                    continue;
                }

                // ACCUMULATE PROGRAM LINES
                lines.Add(input);
            }
        }

        static void RunSource(string source)
        {
            // Stage 1: Lex
            LexorLexer lexer        = new LexorLexer(source);
            List<Token> tokens       = lexer.Tokenize();
            List<string> lexerErrors = lexer.GetErrors();

            if (lexerErrors.Count > 0)
            {
                Console.WriteLine("=== Lexical Warnings ===");
                foreach (string err in lexerErrors)
                    Console.WriteLine(err);
                Console.WriteLine();
            }

            // Stage 2: Parse
            ProgramNode ast;
            try
            {
                LexorInterpreter.Parser.Parser parser =
                    new LexorInterpreter.Parser.Parser(tokens);
                ast = parser.Parse();
            }
            catch (LexorParseException ex)
            {
                Console.WriteLine("Syntax Error: " + ex.Message);
                return;
            }

            // Stage 3: Interpret
            try
            {
                Interpreter.Interpreter interpreter =
                    new Interpreter.Interpreter();
                interpreter.Execute(ast);
            }
            catch (LexorRuntimeException ex)
            {
                Console.WriteLine("Runtime Error: " + ex.Message);
            }
        }
    }
