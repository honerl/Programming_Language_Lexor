using System;
using LexorInterpreter.Shared;

namespace LexorInterpreter.Lexer;
public static class ReservedWords {
    public static readonly Dictionary<String, TokenType> words =
    new Dictionary<String, TokenType>
    {
        {"DECLARE", TokenType.DECLARE },
        {"INT", TokenType.INT },
        {"FLOAT", TokenType.FLOAT },
        {"CHAR", TokenType.CHAR },
        {"BOOL", TokenType.BOOL},
        {"STRING", TokenType.STRING},
        {"TRUE", TokenType.BOOL_LITERAL},
        {"FALSE", TokenType.BOOL_LITERAL},
        {"IF", TokenType.IF},
        {"ELSE", TokenType.ELSE},
        {"FOR", TokenType.FOR},
        {"PRINT", TokenType.PRINT},
        {"SCAN", TokenType.SCAN},
        {"AND", TokenType.AND},
        {"OR", TokenType.OR},
        {"NOT", TokenType.NOT}
    };

    public static TokenType GetTokenType(String word)
    {
        return words.TryGetValue(word, out var type) ? type : TokenType.IDENTIFIER;
    }

    
}
