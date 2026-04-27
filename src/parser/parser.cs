using System;
using System.Collections.Generic;

namespace LexorInterpreter.Parser;
using LexorInterpreter.Shared;

public abstract class ASTNode{}

public class ProgramNode : ASTNode
{
    public List<ASTNode> Declarations = new List<ASTNode>();
    public List<ASTNode> Statements = new List<ASTNode>();
}

public class VarInitNode : ASTNode
{
    public string Name;
    public ASTNode InitValue;
}

public class AssingNode : ASTNode
{
    public string variableName;
    public ASTNode Value;
}

public class ScanNode : ASTNode
{
    public List<string> Variables = new List<string>();
}

public class LiteralNode : ASTNode
{
    public object Value;
}

public class VariableNode : ASTNode
{
    public string Op;
    public ASTNode Left;
    public ASTNode Right;
}

public class UnaryOpNode : ASTNode
{
    public string Op;
    public ASTNode Operand;
}

public class NewlineNode : ASTNode { }

public class EscapeNode : ASTNode
{
    public string Content;
}

 
    public class Parser
    {
        private List<Token> _tokens;
        private int         _current;
 
        public Parser(List<Token> tokens)
        {
            _tokens  = tokens;
            _current = 0;
        }
 
        // ── Public Entry ─────────────────────────────────────────
 
        public ProgramNode Parse()
        {
            ProgramNode program = new ProgramNode();
 
            SkipNewlines();
            Expect(TokenType.SCRIPT_AREA);
            SkipNewlines();
            Expect(TokenType.START_SCRIPT);
            SkipNewlines();
 
            // Declarations must come first
            while (Check(TokenType.DECLARE) || Check(TokenType.COMMENT))
            {
                if (Check(TokenType.COMMENT)) { Advance(); SkipNewlines(); continue; }
                program.Declarations.Add(ParseDeclare());
                SkipNewlines();
            }
 
            // Executable statements
            while (!Check(TokenType.END_SCRIPT) && !Check(TokenType.EOF))
            {
                if (Check(TokenType.COMMENT))  { Advance(); SkipNewlines(); continue; }
                if (Check(TokenType.NEWLINE))  { Advance(); continue; }
 
                ASTNode stmt = ParseStatement();
                if (stmt != null)
                    program.Statements.Add(stmt);
 
                SkipNewlines();
            }
 
            Expect(TokenType.END_SCRIPT);
            return program;
        }
 
        // ── Statement Dispatcher ─────────────────────────────────
 
        private ASTNode ParseStatement()
        {
            if (Check(TokenType.PRINT))      return ParsePrint();
            if (Check(TokenType.SCAN))       return ParseScan();
            if (Check(TokenType.IDENTIFIER)) return ParseAssign();
 
            throw new LexorParseException(string.Format(
                "Unexpected token '{0}' ({1}) at Line {2}.",
                Peek().Value, Peek().Type, Peek().Line));
        }
 
        // ── DECLARE ──────────────────────────────────────────────
 
        private DeclareNode ParseDeclare()
        {
            Expect(TokenType.DECLARE);
 
            DeclareNode node = new DeclareNode();
            node.DataType = ParseDataType();
 
            // First variable (required)
            node.Variables.Add(ParseVarInit(node.DataType));
 
            // Additional variables separated by comma
            while (Check(TokenType.COMMA))
            {
                Advance(); // consume ,
                node.Variables.Add(ParseVarInit(node.DataType));
            }
 
            SkipNewlines();
            return node;
        }
 
        private string ParseDataType()
        {
            Token t = Peek();
            if (t.Type == TokenType.INT   || t.Type == TokenType.FLOAT ||
                t.Type == TokenType.CHAR  || t.Type == TokenType.BOOL)
            {
                Advance();
                return t.Value; // "INT", "FLOAT", "CHAR", "BOOL"
            }
            throw new LexorParseException(string.Format(
                "Expected data type (INT/FLOAT/CHAR/BOOL) but got '{0}' at Line {1}.",
                t.Value, t.Line));
        }
 
        private VarInitNode ParseVarInit(string dataType)
        {
            Token nameToken = Expect(TokenType.IDENTIFIER);
            VarInitNode node = new VarInitNode();
            node.Name = nameToken.Value;
 
            if (Check(TokenType.ASSIGN))
            {
                Advance(); // consume =
                node.InitValue = ParseLiteralForDeclaration(dataType);
            }
 
            return node;
        }
 
        /// <summary>
        /// For declarations, only literals are allowed as initial values
        /// (e.g. DECLARE INT x=5, DECLARE CHAR c='a', DECLARE BOOL b="TRUE")
        /// </summary>
        private ASTNode ParseLiteralForDeclaration(string dataType)
        {
            Token t = Peek();
 
            if (t.Type == TokenType.INT_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = int.Parse(t.Value) };
            }
            if (t.Type == TokenType.FLOAT_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = double.Parse(t.Value) };
            }
            if (t.Type == TokenType.CHAR_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = t.Value[0] };
            }
            if (t.Type == TokenType.BOOL_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = t.Value == "TRUE" };
            }
            if (t.Type == TokenType.STRING_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = t.Value };
            }
            // Allow unary minus for numbers: DECLARE INT x=-5
            if (t.Type == TokenType.MINUS)
            {
                Advance();
                ASTNode operand = ParseLiteralForDeclaration(dataType);
                return new UnaryOpNode { Op = "-", Operand = operand };
            }
 
            throw new LexorParseException(string.Format(
                "Expected literal value for declaration but got '{0}' at Line {1}.",
                t.Value, t.Line));
        }
 
        // ── ASSIGN ───────────────────────────────────────────────
 
        private ASTNode ParseAssign()
        {
            // Handles chained assignment: x=y=4
            string varName = Expect(TokenType.IDENTIFIER).Value;
            Expect(TokenType.ASSIGN);
 
            // Check for chained assignment (e.g. x=y=4)
            if (Check(TokenType.IDENTIFIER) && PeekAhead(1).Type == TokenType.ASSIGN)
            {
                // Recursively parse the right side as another assignment
                ASTNode rhs = ParseAssign();
                return new AssignNode { VariableName = varName, Value = rhs };
            }
 
            ASTNode value = ParseExpression();
            return new AssignNode { VariableName = varName, Value = value };
        }
 
        // ── PRINT ────────────────────────────────────────────────
 
        private PrintNode ParsePrint()
        {
            Expect(TokenType.PRINT);
            Expect(TokenType.COLON);
 
            PrintNode node = new PrintNode();
            node.Parts.Add(ParsePrintPart());
 
            while (Check(TokenType.AMPERSAND))
            {
                Advance(); // consume &
                node.Parts.Add(ParsePrintPart());
            }
 
            return node;
        }
 
        private ASTNode ParsePrintPart()
        {
            Token t = Peek();
 
            // $ = newline
            if (t.Type == TokenType.DOLLAR)
            {
                Advance();
                return new NewlineNode();
            }
 
            // [x] = escape code — content between brackets printed literally
            if (t.Type == TokenType.LBRACKET)
            {
                Advance(); // consume outer [
                string content = "";
                // Special case: []] means literal "]"
                // After consuming [, if we see ] followed by another ] then content is "]"
                if (Check(TokenType.RBRACKET) && PeekAhead(1).Type == TokenType.RBRACKET)
                {
                    content = "]";
                    Advance(); // consume the content ]
                    Advance(); // consume closing ]
                }
                else
                {
                    // Collect all tokens until closing ]
                    // UNKNOWN tokens (like #) are included as their character value
                    while (!Check(TokenType.RBRACKET) && !Check(TokenType.EOF) && !Check(TokenType.NEWLINE))
                    {
                        content += Peek().Value;
                        Advance();
                    }
                    Expect(TokenType.RBRACKET);
                }
                return new EscapeNode { Content = content };
            }
 
            return ParseExpression();
        }
 
        // ── SCAN ─────────────────────────────────────────────────
 
        private ScanNode ParseScan()
        {
            Expect(TokenType.SCAN);
            Expect(TokenType.COLON);
 
            ScanNode node = new ScanNode();
            node.Variables.Add(Expect(TokenType.IDENTIFIER).Value);
 
            while (Check(TokenType.COMMA))
            {
                Advance();
                node.Variables.Add(Expect(TokenType.IDENTIFIER).Value);
            }
 
            return node;
        }
 
        // ── Expression Parsing (Recursive Descent) ───────────────
        // Precedence (lowest → highest):
        //   OR
        //   AND
        //   NOT
        //   ==  <>
        //   >  <  >=  <=
        //   +  -
        //   *  /  %
        //   Unary +/-
        //   Primary (literal, variable, parenthesized expr)
 
        private ASTNode ParseExpression()
        {
            return ParseOr();
        }
 
        private ASTNode ParseOr()
        {
            ASTNode left = ParseAnd();
            while (Check(TokenType.OR))
            {
                Advance();
                ASTNode right = ParseAnd();
                left = new BinaryOpNode { Op = "OR", Left = left, Right = right };
            }
            return left;
        }
 
        private ASTNode ParseAnd()
        {
            ASTNode left = ParseNot();
            while (Check(TokenType.AND))
            {
                Advance();
                ASTNode right = ParseNot();
                left = new BinaryOpNode { Op = "AND", Left = left, Right = right };
            }
            return left;
        }
 
        private ASTNode ParseNot()
        {
            if (Check(TokenType.NOT))
            {
                Advance();
                return new UnaryOpNode { Op = "NOT", Operand = ParseNot() };
            }
            return ParseEquality();
        }
 
        private ASTNode ParseEquality()
        {
            ASTNode left = ParseComparison();
            while (Check(TokenType.EQUAL) || Check(TokenType.NOT_EQUAL))
            {
                string op = Peek().Value;
                Advance();
                ASTNode right = ParseComparison();
                left = new BinaryOpNode { Op = op, Left = left, Right = right };
            }
            return left;
        }
 
        private ASTNode ParseComparison()
        {
            ASTNode left = ParseAddSub();
            while (Check(TokenType.GREATER)    || Check(TokenType.LESS) ||
                   Check(TokenType.GREATER_EQ) || Check(TokenType.LESS_EQ))
            {
                string op = Peek().Value;
                Advance();
                ASTNode right = ParseAddSub();
                left = new BinaryOpNode { Op = op, Left = left, Right = right };
            }
            return left;
        }
 
        private ASTNode ParseAddSub()
        {
            ASTNode left = ParseMulDiv();
            while (Check(TokenType.PLUS) || Check(TokenType.MINUS))
            {
                string op = Peek().Value;
                Advance();
                ASTNode right = ParseMulDiv();
                left = new BinaryOpNode { Op = op, Left = left, Right = right };
            }
            return left;
        }
 
        private ASTNode ParseMulDiv()
        {
            ASTNode left = ParseUnary();
            while (Check(TokenType.MULTIPLY) || Check(TokenType.DIVIDE) || Check(TokenType.MODULO))
            {
                string op = Peek().Value;
                Advance();
                ASTNode right = ParseUnary();
                left = new BinaryOpNode { Op = op, Left = left, Right = right };
            }
            return left;
        }
 
        private ASTNode ParseUnary()
        {
            if (Check(TokenType.MINUS))
            {
                Advance();
                return new UnaryOpNode { Op = "-", Operand = ParseUnary() };
            }
            if (Check(TokenType.PLUS))
            {
                Advance();
                return new UnaryOpNode { Op = "+", Operand = ParseUnary() };
            }
            return ParsePrimary();
        }
 
        private ASTNode ParsePrimary()
        {
            Token t = Peek();
 
            if (t.Type == TokenType.INT_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = int.Parse(t.Value) };
            }
            if (t.Type == TokenType.FLOAT_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = double.Parse(t.Value) };
            }
            if (t.Type == TokenType.CHAR_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = t.Value[0] };
            }
            if (t.Type == TokenType.BOOL_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = t.Value == "TRUE" };
            }
            if (t.Type == TokenType.STRING_LITERAL)
            {
                Advance();
                return new LiteralNode { Value = t.Value };
            }
            if (t.Type == TokenType.IDENTIFIER)
            {
                Advance();
                return new VariableNode { Name = t.Value };
            }
            if (t.Type == TokenType.LPAREN)
            {
                Advance(); // consume (
                ASTNode expr = ParseExpression();
                Expect(TokenType.RPAREN);
                return expr;
            }
 
            throw new LexorParseException(string.Format(
                "Unexpected token '{0}' ({1}) at Line {2}.",
                t.Value, t.Type, t.Line));
        }
 
        // ── Scanner Helpers ──────────────────────────────────────
 
        private Token Peek()
        {
            return _tokens[_current];
        }
 
        private Token PeekAhead(int offset)
        {
            int idx = _current + offset;
            return idx < _tokens.Count ? _tokens[idx] : _tokens[_tokens.Count - 1];
        }
 
        private Token Advance()
        {
            Token t = _tokens[_current];
            if (_current < _tokens.Count - 1) _current++;
            return t;
        }
 
        private bool Check(TokenType type)
        {
            return Peek().Type == type;
        }
 
        private Token Expect(TokenType type)
        {
            Token t = Peek();
            if (t.Type != type)
                throw new LexorParseException(string.Format(
                    "Expected {0} but got '{1}' ({2}) at Line {3}.",
                    type, t.Value, t.Type, t.Line));
            return Advance();
        }
 
        private void SkipNewlines()
        {
            while (Check(TokenType.NEWLINE) || Check(TokenType.COMMENT))
                Advance();
        }
    }
 
    // ── Parse Exception ──────────────────────────────────────────
 
    public class LexorParseException : Exception
    {
        public LexorParseException(string message) : base(message) { }
    }



