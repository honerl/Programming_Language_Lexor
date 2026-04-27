using System.Text;
using LexorInterpreter.Shared;

namespace LexerInterpreter.Lexer;
public partial class LexorLexer
{
    // Helper Functions;
    private char Peek()
	{
		return _pos < _source.Length ? _source[_pos] : '\0';
	}
	
	private char PeekNext()
	{
		return (_pos + 1) < _source.Length ? _source[_pos + 1] : '\0';
	}
	
	private char Advance()
	{
		char c = _source[_pos++];
		if (c == '\n') {_line++; _column = 1; }
		else { _column++;}
		
		return c;
	}
	
	
	private bool Match(char expected)
	{
		if(_pos < _source.Length && _source[_pos] == expected)
		{
			Advance();
			return true;
        }
		return false;
	}
	
	private void AddToken(TokenType type, string value, int line, int col)
	{
		_tokens.Add(new Token(type, value, line, col));
	}
	
	private void AddError(string msg, int line, int col)
	{
		_errors.Add(string.Format("Error at Line {0}, Col {1} : {2}", line, col, msg));
	}
	
	private void SkipWhitespace()
	{
		while (_pos < _source.Length && (Peek() == ' ' || Peek() == '\t'))
		Advance();
	}
	
	private string PeekRestOfLine()
	{
		int i = _pos;
		var sb = new StringBuilder();
		while (i < _source.Length && _source[i] == ' ') i++;
		while (i < _source.Length && _source[i] != '\n' && _source[i] != '\r')
			sb.Append(_source[i++]);
		return sb.ToString();
	}
	
	private void ConsumeWord(String word)
	{
		SkipWhitespace();
		foreach (char c in word)
			if (_pos < _source.Length && Peek() == c) Advance();
	}
}