// Lexical Analysis

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LexerInterpreter.Lexer;
public partial class LexorLexer 
{
	private string _source;
	private int _pos;
	private int _line;
	private int _column;
	private List<Token> _tokens;
	private List<String> _errors;
	
	public LexorLexer(string source)
	{
		_source = source;
		_pos = 0;
		_line = 1;
		_column = 1;
		_tokens = new List<Token>();
		_errors = new List<String>();
	}

    public List<string> GetErrors() { return _errors; }

    public List<Token> Tokenize()
    {
        while(_pos <_source.Length)
        {
            SkipWhitespace();
            if(_pos >= _source.Length) break;

            int tokenLine = _line;
            int tokenCol = _column;

            char c = Peek();
            if (c == '%' && PeekNext() == '%') ScanComment(tokenLine, tokenCol);
            else if (c == '"') ScanStringLiteral(tokenLine, tokenCol);
            else if (c == '\'') ScanCharLiteral(tokenLine, tokenCol);
            else if (char.IsDigit(c)) ScanNumber(tokenLine, tokenCol);
            else if (char.IsLetter(c) || c == '_') ScanIdentifierOrKeyword(tokenLine, tokenCol);
            else ScanSymbol(tokenLine, tokenCol);
        }

        AddToken(TokenType.EOF, "EOF", _line, _column);
        return _tokens;

    }

	
	
	
	
	
	



}