using System;
using System.Collections.Generic;
using LexorInterpreter.Shared;

namespace LexorInterpreter.Parser.Scanner
{
    /// <summary>
    /// Wraps the token list produced by the Lexer and provides
    /// clean navigation methods for the Parser to consume.
    /// The Parser never touches the raw List&lt;Token&gt; directly.
    /// </summary>
    public class Scanner
    {
        private List<Token> _tokens;
        private int         _current;

        public Scanner(List<Token> tokens)
        {
            _tokens  = tokens;
            _current = 0;
        }

        // ── Navigation ───────────────────────────────────────────

        /// <summary>Returns the current token without consuming it.</summary>
        public Token Peek()
        {
            return _tokens[_current];
        }

        /// <summary>Returns a token ahead by offset without consuming.</summary>
        public Token PeekAhead(int offset)
        {
            int idx = _current + offset;
            return idx < _tokens.Count ? _tokens[idx] : _tokens[_tokens.Count - 1];
        }

        /// <summary>Consumes and returns the current token.</summary>
        public Token Consume()
        {
            Token t = _tokens[_current];
            if (_current < _tokens.Count - 1) _current++;
            return t;
        }

        /// <summary>
        /// Consumes the current token if it matches the expected type.
        /// Throws a ScannerException if the type does not match.
        /// </summary>
        public Token Expect(TokenType type)
        {
            Token t = Peek();
            if (t.Type != type)
                throw new ScannerException(string.Format(
                    "Expected {0} but got '{1}' ({2}) at Line {3}.",
                    type, t.Lexeme, t.Type, t.Line));
            return Consume();
        }

        /// <summary>Returns true if the current token matches the given type.</summary>
        public bool Check(TokenType type)
        {
            return Peek().Type == type;
        }

        /// <summary>
        /// Consumes the current token and returns true if it matches.
        /// Does nothing and returns false otherwise.
        /// </summary>
        public bool Match(TokenType type)
        {
            if (Check(type)) { Consume(); return true; }
            return false;
        }

        /// <summary>Returns true if the current token is EOF.</summary>
        public bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        /// <summary>Skips over NEWLINE and COMMENT tokens.</summary>
        public void SkipNewlines()
        {
            while (Check(TokenType.NEWLINE) || Check(TokenType.COMMENT))
                Consume();
        }

        /// <summary>Returns the current position index (for error reporting).</summary>
        public int CurrentLine()
        {
            return Peek().Line;
        }
    }

    // ── Scanner Exception ────────────────────────────────────────

    public class ScannerException : Exception
    {
        public ScannerException(string message) : base(message) { }
    }
}