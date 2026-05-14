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

            //  Interactive menu mode
            ShowMainMenu();
        }

        static void ShowMainMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=============================================");
                Console.WriteLine("        LEXOR Interpreter v1.0");
                Console.WriteLine("=============================================");
                Console.WriteLine("Choose an option:");
                Console.WriteLine("  1 - REPL Mode (type code line by line)");
                Console.WriteLine("  2 - Open a file");
                Console.WriteLine("  3 - Paste code directly");
                Console.WriteLine("  4 - Exit");
                Console.WriteLine("=============================================");
                Console.Write("Enter choice (1-4): ");
                
                string choice = Console.ReadLine()?.Trim() ?? "";

                switch (choice)
                {
                    case "1":
                        RunREPL();
                        break;
                    case "2":
                        OpenFileAndRun();
                        break;
                    case "3":
                        PasteCodeMode();
                        break;
                    case "4":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Press Enter to continue...");
                        Console.ReadLine();
                        break;
                }
            }
        }

        static void OpenFileAndRun()
        {
            Console.Clear();
            Console.WriteLine("=============================================");
            Console.WriteLine("        OPEN FILE");
            Console.WriteLine("=============================================");
            Console.WriteLine("Current directory: {0}", Directory.GetCurrentDirectory());
            Console.WriteLine("\nAvailable .lexor files:");
            
            string[] lexorFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.lexor", SearchOption.AllDirectories);
            
            if (lexorFiles.Length == 0)
            {
                Console.WriteLine("(No .lexor files found)");
                Console.WriteLine("\nEnter full file path:");
                string filePath = Console.ReadLine()?.Trim() ?? "";
                
                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("Cancelled. Press Enter to return...");
                    Console.ReadLine();
                    return;
                }

                filePath = filePath.Replace("\"", ""); // Remove quotes if pasted

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("File not found: {0}", filePath);
                    Console.WriteLine("Press Enter to return...");
                    Console.ReadLine();
                    return;
                }

                RunSource(File.ReadAllText(filePath));
            }
            else
            {
                for (int i = 0; i < lexorFiles.Length; i++)
                {
                    Console.WriteLine("  {0} - {1}", i + 1, lexorFiles[i]);
                }
                
                Console.WriteLine("\nEnter file number or full path:");
                string input = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Cancelled. Press Enter to return...");
                    Console.ReadLine();
                    return;
                }

                string filePath;

                if (int.TryParse(input, out int fileNum) && fileNum >= 1 && fileNum <= lexorFiles.Length)
                {
                    filePath = lexorFiles[fileNum - 1];
                }
                else
                {
                    filePath = input.Replace("\"", "");
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine("File not found: {0}", filePath);
                        Console.WriteLine("Press Enter to return...");
                        Console.ReadLine();
                        return;
                    }
                }

                RunSource(File.ReadAllText(filePath));
            }

            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }

        static void PasteCodeMode()
        {
            Console.Clear();
            Console.WriteLine("=============================================");
            Console.WriteLine("        PASTE CODE MODE");
            Console.WriteLine("=============================================");
            Console.WriteLine("Paste your LEXOR code below.");
            Console.WriteLine("Enter an empty line to execute:");
            Console.WriteLine();

            List<string> lines = new List<string>();
            while (true)
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;
                lines.Add(line);
            }

            if (lines.Count == 0)
            {
                Console.WriteLine("No code entered. Press Enter to return...");
                Console.ReadLine();
                return;
            }

            string source = string.Join("\n", lines);
            Console.WriteLine("\n--- Output ---");
            RunSource(source);
            Console.WriteLine("--------------");
            Console.WriteLine("\nPress Enter to return to menu...");
            Console.ReadLine();
        }
        

        static void RunREPL()
        {
            Console.Clear();
            Console.WriteLine("=============================================");
            Console.WriteLine("        LEXOR Interpreter v1.0 - REPL Mode");
            Console.WriteLine("=============================================");
            Console.WriteLine("Write your LEXOR program line by line.");
            Console.WriteLine("Commands:");
            Console.WriteLine("  RUN   - execute the program");
            Console.WriteLine("  CLEAR - clear and start over");
            Console.WriteLine("  LIST  - show current program");
            Console.WriteLine("  EXIT  - return to menu");
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

                // COMMANDS
                if (trimmed == "EXIT")
                {
                    Console.WriteLine("Returning to menu...\n");
                    Console.ReadLine();
                    return;
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
