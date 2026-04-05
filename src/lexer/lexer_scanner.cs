using System.Text;
namespace LexerInterpreter.Lexer;
public partial class LexorLexer
{
    	
	// Scan Methods
	private void ScanComment(int tokenLine, int tokenCol)
	{
		Advance(); Advance();
		var sb = new StringBuilder();
		while (_pos < _source.Length && Peek() != '\n' && Peek() != '\r')
			sb.Append(Advance());
		AddToken(TokenType.COMMENT, "%%" + sb.ToString().Trim(), tokenLine, tokenCol);
	}
	
	private void ScanStringLiteral(int tokenLine, int tokenCol)
	{
		Advance();
		var sb = new StringBuilder();
		while(_pos < _source.Length && Peek() != '"')
		{
			if(Peek() == '\n' || Peek() == '\r')
			{
				AddError("Undetermined String Literal", tokenLine, tokenCol);
				return;
			}
			sb.Append(Advance());
		}
		
		if (_pos >= _source.Length)
		{
			AddError("Undetermined string literal", tokenLine, tokenCol);
			return;
		}
		Advance();
		string val = sb.ToString();
		if(val == "TRUE" || val == "FALSE")
			AddToken(TokenType.BOOL_LITERAL, val, tokenLine, tokenCol);
		else
			AddToken(TokenType.STRING_LITERAL, val, tokenLine, tokenCol);
	}
	
	
	private void ScanCharLiteral(int tokenLine, int tokenCol)
	{
		Advance();
		if(_pos >= _source.Length)
		{
			AddError("Undetermined Char Literal", tokenLine, tokenCol);
			return;
		}
		char ch = Advance();
		if(_pos >= _source.Length || Peek() != '\'')
		{
			AddError("Invalid char literal (must be a single character)", tokenLine, tokenCol);
			while(_pos < _source.Length && Peek() != '\'' && Peek() != '\n') Advance();
			if (_pos < _source.Length && Peek() == '\'') Advance();
			return;
		}
		Advance();
		AddToken(TokenType.CHAR_LITERAL, ch.ToString(), tokenLine, tokenCol);
	}
	
	private void ScanNumber(int tokenLine, int tokenCol)
	{
		var sb = new StringBuilder();
		while(_pos < _source.Length && char.IsDigit(Peek()))
			sb.Append(Advance());
		
		if (_pos < _source.Length && Peek() == '.' && char.IsDigit(PeekNext()))
		{
			sb.Append(Advance()); // Consume '.'
			while (_pos < _source.Length && char.IsDigit(Peek()))
				sb.Append(Advance());
			AddToken(TokenType.FLOAT_LITERAL, sb.ToString(), tokenLine, tokenCol);
			
		}
		else
		{
			AddToken(TokenType.INT_LITERAL, sb.ToString(), tokenLine, tokenCol);
		}
	}
	
    private void ScanIdentifierOrKeyword(int tokenLine, int tokenCol)
        {
            var sb = new StringBuilder();
            while (_pos < _source.Length && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
                sb.Append(Advance());

            string word     = sb.ToString();
            string combined = word + " " + PeekRestOfLine().TrimStart();

            // Multi-word keywords — longer matches must come first
            if      (word == "SCRIPT" && combined.StartsWith("SCRIPT AREA"))   { SkipWhitespace(); ConsumeWord("AREA");   AddToken(TokenType.SCRIPT_AREA,  "SCRIPT AREA",  tokenLine, tokenCol); }
            else if (word == "START"  && combined.StartsWith("START SCRIPT"))  { SkipWhitespace(); ConsumeWord("SCRIPT"); AddToken(TokenType.START_SCRIPT, "START SCRIPT", tokenLine, tokenCol); }
            else if (word == "END"    && combined.StartsWith("END SCRIPT"))    { SkipWhitespace(); ConsumeWord("SCRIPT"); AddToken(TokenType.END_SCRIPT,   "END SCRIPT",   tokenLine, tokenCol); }
            else if (word == "START"  && combined.StartsWith("START IF"))      { SkipWhitespace(); ConsumeWord("IF");     AddToken(TokenType.START_IF,     "START IF",     tokenLine, tokenCol); }
            else if (word == "END"    && combined.StartsWith("END IF"))        { SkipWhitespace(); ConsumeWord("IF");     AddToken(TokenType.END_IF,       "END IF",       tokenLine, tokenCol); }
            else if (word == "ELSE"   && combined.StartsWith("ELSE IF"))       { SkipWhitespace(); ConsumeWord("IF");     AddToken(TokenType.ELSE_IF,      "ELSE IF",      tokenLine, tokenCol); }
            else if (word == "START"  && combined.StartsWith("START FOR"))     { SkipWhitespace(); ConsumeWord("FOR");    AddToken(TokenType.START_FOR,    "START FOR",    tokenLine, tokenCol); }
            else if (word == "END"    && combined.StartsWith("END FOR"))       { SkipWhitespace(); ConsumeWord("FOR");    AddToken(TokenType.END_FOR,      "END FOR",      tokenLine, tokenCol); }
            else if (word == "REPEAT" && combined.StartsWith("REPEAT WHEN"))   { SkipWhitespace(); ConsumeWord("WHEN");   AddToken(TokenType.REPEAT_WHEN,  "REPEAT WHEN",  tokenLine, tokenCol); }
            else if (word == "START"  && combined.StartsWith("START REPEAT"))  { SkipWhitespace(); ConsumeWord("REPEAT"); AddToken(TokenType.START_REPEAT, "START REPEAT", tokenLine, tokenCol); }
            else if (word == "END"    && combined.StartsWith("END REPEAT"))    { SkipWhitespace(); ConsumeWord("REPEAT"); AddToken(TokenType.END_REPEAT,   "END REPEAT",   tokenLine, tokenCol); }
            else
            {
                TokenType type;
                if (!ReservedWords.words.TryGetValue(word, out type))
                    type = TokenType.IDENTIFIER;
                AddToken(type, word, tokenLine, tokenCol);
            }
        }
	 private void ScanSymbol(int tokenLine, int tokenCol)
        {
            char c = Peek();

            if (c == '=')
            {
                Advance();
                if (Match('=')) AddToken(TokenType.EQUAL,  "==", tokenLine, tokenCol);
                else            AddToken(TokenType.ASSIGN, "=",  tokenLine, tokenCol);
            }
            else if (c == '<')
            {
                Advance();
                if      (Match('>')) AddToken(TokenType.NOT_EQUAL, "<>", tokenLine, tokenCol);
                else if (Match('=')) AddToken(TokenType.LESS_EQ,   "<=", tokenLine, tokenCol);
                else                 AddToken(TokenType.LESS,       "<", tokenLine, tokenCol);
            }
            else if (c == '>')
            {
                Advance();
                if (Match('=')) AddToken(TokenType.GREATER_EQ, ">=", tokenLine, tokenCol);
                else            AddToken(TokenType.GREATER,     ">", tokenLine, tokenCol);
            }
            else if (c == '+') { Advance(); AddToken(TokenType.PLUS,      "+", tokenLine, tokenCol); }
            else if (c == '-') { Advance(); AddToken(TokenType.MINUS,     "-", tokenLine, tokenCol); }
            else if (c == '*') { Advance(); AddToken(TokenType.MULTIPLY,  "*", tokenLine, tokenCol); }
            else if (c == '/') { Advance(); AddToken(TokenType.DIVIDE,    "/", tokenLine, tokenCol); }
            else if (c == '%') { Advance(); AddToken(TokenType.MODULO,    "%", tokenLine, tokenCol); }
            else if (c == '(') { Advance(); AddToken(TokenType.LPAREN,    "(", tokenLine, tokenCol); }
            else if (c == ')') { Advance(); AddToken(TokenType.RPAREN,    ")", tokenLine, tokenCol); }
            else if (c == '[') { Advance(); AddToken(TokenType.LBRACKET,  "[", tokenLine, tokenCol); }
            else if (c == ']') { Advance(); AddToken(TokenType.RBRACKET,  "]", tokenLine, tokenCol); }
            else if (c == ':') { Advance(); AddToken(TokenType.COLON,     ":", tokenLine, tokenCol); }
            else if (c == ',') { Advance(); AddToken(TokenType.COMMA,     ",", tokenLine, tokenCol); }
            else if (c == '&') { Advance(); AddToken(TokenType.AMPERSAND, "&", tokenLine, tokenCol); }
            else if (c == '$') { Advance(); AddToken(TokenType.DOLLAR,    "$", tokenLine, tokenCol); }
            else if (c == '\n')
            {
                Advance();
                AddToken(TokenType.NEWLINE, "\\n", tokenLine, tokenCol);
            }
            else if (c == '\r')
            {
                Advance();
                if (Peek() == '\n') Advance();
                AddToken(TokenType.NEWLINE, "\\n", tokenLine, tokenCol);
            }
            else
            {
                AddError(string.Format("Unexpected character '{0}'", c), tokenLine, tokenCol);
                Advance();
            }
        }
}