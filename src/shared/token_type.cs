
namespace LexorInterpreter.Shared;
public enum TokenType
{
    // Program Structure
    SCRIPT_AREA, START_SCRIPT, END_SCRIPT,

    // Declarations
    DECLARE,

    // Data Types
    INT, FLOAT, CHAR, BOOL, STRING,

    // Control Flow
    IF, START_IF, END_IF, ELSE, ELSE_IF,
    FOR, START_FOR, END_FOR,
    REPEAT_WHEN, START_REPEAT, END_REPEAT,

    // I/O
    PRINT, SCAN,

    // LOGICAL OPERATORS
    AND, OR, NOT,

    // ARITHMETIC / RELATIONAL OPERATORS
    PLUS,           // +
    MINUS,          // -
    MULTIPLY,       // *
    DIVIDE,         // /
    MODULO,         // %
    GREATER,        // >
    LESS,
    GREATER_EQ,     // >=
    LESS_EQ,        // <=
    EQUAL,          // ==
    NOT_EQUAL,      // !=
    ASSIGN,         // =

    // SPECIAL SYMBOLS
    LPAREN,         // (
    RPAREN,         // )
    LBRACKET,       // [
    RBRACKET,       // ]
    COLON,          // :
    COMMA,          // ,
    AMPERSAND,      // &
    DOLLAR,         // $
    COMMENT,         // %% 

    // Literals
    INT_LITERAL,
    FLOAT_LITERAL,
    CHAR_LITERAL,
    STRING_LITERAL,
    BOOL_LITERAL,

    // IDENTIFIER
    IDENTIFIER,

    // SPECIAL
    NEWLINE,
    EOF,
    UNKNOWN


}