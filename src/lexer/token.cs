using System;

namespace LexerInterpreter.Lexer;
public class Token
{
    public TokenType Type;
    public String Lexeme;
    public int Line;
    public int Column;
    public Token(TokenType type, String lexeme, int line, int column)
    {
        this.Type = type;
        this.Lexeme = lexeme;
        this.Line = line;
        this.Column = column;
    }

    // public Token(TokenType eND_SCRIPT, string v, object value, int line)
    // {
    //     this.eND_SCRIPT = eND_SCRIPT;
    //     this.v = v;
    //     this.value = value;
    //     Line = line;
    // }

    public override String ToString()
    {
        return string.Format("{0, -20} | {1, -28} | Line {2, -4} | Col {3}",
            Type , "\"" + Lexeme +  "\"", Line, Column);
    }

}