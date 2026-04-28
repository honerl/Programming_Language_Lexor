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
            string source;

            // ── Load source ──────────────────────────────────────
            if (args.Length > 0)
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine("Error: File '{0}' not found.", args[0]);
                    return;
                }
                source = File.ReadAllText(args[0]);
            }
            else
            {
                // Default sample from the LEXOR spec
                source =
                    "%% this is a sample program in LEXOR\n" +
                    "SCRIPT AREA\n" +
                    "START SCRIPT\n" +
                    "DECLARE INT x, y, z=5\n" +
                    "DECLARE CHAR a_1='n'\n" +
                    "DECLARE BOOL t=\"TRUE\"\n" +
                    "x=y=4\n" +
                    "a_1='c'\n" +
                    "%% this is a comment\n" +
                    "PRINT: x & t & z & $ & a_1 & [] & \"last\"\n" +
                    "END SCRIPT\n";
            }

            // ── Stage 1: Lexical Analysis ────────────────────────
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

            // ── Stage 2: Parsing ─────────────────────────────────
            ProgramNode ast;
            try
            {
                Parser.Parser parser =
                    new Parser.Parser(tokens);
                ast = parser.Parse();
            }
            catch (LexorParseException ex)
            {
                Console.WriteLine("=== Syntax Error ===");
                Console.WriteLine(ex.Message);
                return;
            }

            // ── Stage 3: Interpretation ──────────────────────────
            try
            {
                Interpreter.Interpreter interpreter =
                    new Interpreter.Interpreter();
                interpreter.Execute(ast);
            }
            catch (LexorRuntimeException ex)
            {
                Console.WriteLine("=== Runtime Error ===");
                Console.WriteLine(ex.Message);
            }
        }
    }
