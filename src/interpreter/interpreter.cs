using System;
using System.Collections.Generic;
using System.Text;

namespace LexorInterpreter.Interpreter
{
    using LexorInterpreter.Parser;

    /// <summary>
    /// Walks the AST produced by the Parser and executes each node.
    /// Supports: DECLARE, ASSIGN (chained), PRINT, SCAN,
    ///           Unary (+/-/NOT), Arithmetic, Logical, Relational ops.
    /// </summary>
    public class Interpreter
    {
        private SymbolTable _symbols;

        public Interpreter()
        {
            _symbols = new SymbolTable();
        }

        // ═════════════════════════════════════════════════════════
        //  Public Entry
        // ═════════════════════════════════════════════════════════

        public void Execute(ProgramNode program)
        {
            // Phase 1: declarations
            foreach (ASTNode node in program.Declarations)
                ExecuteNode(node);

            // Phase 2: statements
            foreach (ASTNode node in program.Statements)
                ExecuteNode(node);
        }

        // ═════════════════════════════════════════════════════════
        //  Node Dispatcher
        // ═════════════════════════════════════════════════════════

        private void ExecuteNode(ASTNode node)
        {
            if      (node is DeclareNode) ExecuteDeclare((DeclareNode)node);
            else if (node is AssignNode)  ExecuteAssign((AssignNode)node);
            else if (node is PrintNode)   ExecutePrint((PrintNode)node);
            else if (node is ScanNode)    ExecuteScan((ScanNode)node);
            else
                throw new LexorRuntimeException(
                    string.Format("Unknown statement node: {0}", node.GetType().Name));
        }

        // ═════════════════════════════════════════════════════════
        //  DECLARE
        // ═════════════════════════════════════════════════════════

        private void ExecuteDeclare(DeclareNode node)
        {
            foreach (VarInitNode varInit in node.Variables)
            {
                object initValue = null;
                if (varInit.InitValue != null)
                    initValue = Evaluate(varInit.InitValue);
                // Pass the line number so duplicate errors show exact location
                _symbols.Declare(varInit.Name, node.DataType, initValue, varInit.Line);
            }
        }

        // ═════════════════════════════════════════════════════════
        //  ASSIGN  (supports chained: x=y=4)
        // ═════════════════════════════════════════════════════════

        private void ExecuteAssign(AssignNode node)
        {
            object value = Evaluate(node.Value);
            _symbols.Set(node.VariableName, value);
        }

        // ═════════════════════════════════════════════════════════
        //  PRINT
        //  Syntax: PRINT: <expr> & <expr> & ...
        //  $  → newline (flush current line, start new)
        //  [x]→ escape — print x literally
        //  Uses Console.Write so output stays on same line until $ or end
        // ═════════════════════════════════════════════════════════

        private void ExecutePrint(PrintNode node)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ASTNode part in node.Parts)
            {
                if (part is NewlineNode)
                {
                    // $ : flush current buffer as a line, then continue on next
                    Console.WriteLine(sb.ToString());
                    sb.Clear();
                }
                else if (part is EscapeNode)
                {
                    sb.Append(((EscapeNode)part).Content);
                }
                else
                {
                    object val = Evaluate(part);
                    sb.Append(FormatValue(val));
                }
            }

            // Flush remaining content
            // Use Write (no newline) so PRINT prompts sit on same line as SCAN input
            Console.Write(sb.ToString());

            // If this PRINT ends with a string literal (prompt) we want the
            // cursor to stay there — the \n will come after the user presses Enter.
            // For all other cases (displaying results) we add a newline.
            // Heuristic: if the last part was a string literal prompt, no newline.
            // Simpler rule from spec: PRINT always ends with newline EXCEPT when
            // followed immediately by SCAN — we can't know that ahead of time,
            // so we always end with newline and the prompt just goes to next line.
            Console.WriteLine();
        }

        // ═════════════════════════════════════════════════════════
        //  SCAN
        //  Syntax: SCAN: var1[, var2, ...]
        //  Single variable  → reads one line as the value
        //  Multiple vars    → reads one comma-separated line
        // ═════════════════════════════════════════════════════════

        private void ExecuteScan(ScanNode node)
        {
            if (node.Variables.Count == 1)
            {
                // Single variable — read the whole line
                string varName = node.Variables[0];
                string raw     = (Console.ReadLine() ?? "").Trim();
                string varType = _symbols.GetType(varName);
                _symbols.Set(varName, ParseInput(raw, varType, varName));
            }
            else
            {
                // Multiple variables — comma-separated on one line
                string   line  = Console.ReadLine() ?? "";
                string[] parts = line.Split(',');

                for (int i = 0; i < node.Variables.Count; i++)
                {
                    string varName = node.Variables[i];
                    string raw     = i < parts.Length ? parts[i].Trim() : "";
                    string varType = _symbols.GetType(varName);
                    _symbols.Set(varName, ParseInput(raw, varType, varName));
                }
            }
        }

        // ── Input parsing with type checking ─────────────────────

        private object ParseInput(string raw, string type, string varName)
        {
            try
            {
                switch (type)
                {
                    case "INT":
                        int intVal;
                        if (!int.TryParse(raw, out intVal))
                            throw new LexorRuntimeException(
                                string.Format("Expected an integer for '{0}' but got '{1}'.", varName, raw));
                        return intVal;

                    case "FLOAT":
                        double dblVal;
                        if (!double.TryParse(raw, out dblVal))
                            throw new LexorRuntimeException(
                                string.Format("Expected a number for '{0}' but got '{1}'.", varName, raw));
                        return dblVal;

                    case "CHAR":
                        if (raw.Length != 1)
                            throw new LexorRuntimeException(
                                string.Format("CHAR input for '{0}' must be exactly one character.", varName));
                        return raw[0];

                    case "BOOL":
                        string upper = raw.ToUpper();
                        if (upper == "TRUE")  return true;
                        if (upper == "FALSE") return false;
                        throw new LexorRuntimeException(
                            string.Format("BOOL input for '{0}' must be TRUE or FALSE.", varName));

                    default:
                        return raw;
                }
            }
            catch (LexorRuntimeException) { throw; }
            catch (Exception ex)
            {
                throw new LexorRuntimeException(
                    string.Format("Input error for '{0}': {1}", varName, ex.Message));
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Evaluator — resolves any expression node to a value
        // ═════════════════════════════════════════════════════════

        public object Evaluate(ASTNode node)
        {
            if (node is LiteralNode)
                return ((LiteralNode)node).Value;

            if (node is VariableNode)
                return _symbols.Get(((VariableNode)node).Name);

            if (node is AssignNode)
            {
                // Chained assignment: x=y=4
                AssignNode assign = (AssignNode)node;
                object val = Evaluate(assign.Value);
                _symbols.Set(assign.VariableName, val);
                return _symbols.Get(assign.VariableName);
            }

            if (node is UnaryOpNode)  return EvaluateUnary((UnaryOpNode)node);
            if (node is BinaryOpNode) return EvaluateBinary((BinaryOpNode)node);

            throw new LexorRuntimeException(
                string.Format("Cannot evaluate node of type {0}.", node.GetType().Name));
        }

        // ═════════════════════════════════════════════════════════
        //  Unary Operators:  +(x)  -(x)  NOT(x)
        // ═════════════════════════════════════════════════════════

        private object EvaluateUnary(UnaryOpNode node)
        {
            object val = Evaluate(node.Operand);

            switch (node.Op)
            {
                case "-":
                    if (val is int)    return -(int)val;
                    if (val is double) return -(double)val;
                    throw new LexorRuntimeException(
                        string.Format("Unary '-' cannot be applied to type {0}.", val.GetType().Name));

                case "+":
                    if (val is int || val is double) return val;
                    throw new LexorRuntimeException(
                        string.Format("Unary '+' cannot be applied to type {0}.", val.GetType().Name));

                case "NOT":
                    if (val is bool) return !(bool)val;
                    throw new LexorRuntimeException(
                        "NOT operator requires a BOOL expression.");

                default:
                    throw new LexorRuntimeException(
                        string.Format("Unknown unary operator '{0}'.", node.Op));
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Binary Operators
        //  Arithmetic : +  -  *  /  %
        //  Relational : >  <  >=  <=  ==  <>
        //  Logical    : AND  OR
        // ═════════════════════════════════════════════════════════

        private object EvaluateBinary(BinaryOpNode node)
        {
            object left  = Evaluate(node.Left);
            object right = Evaluate(node.Right);

            switch (node.Op)
            {
                case "+":   return ArithmeticOp(left, right, "+");
                case "-":   return ArithmeticOp(left, right, "-");
                case "*":   return ArithmeticOp(left, right, "*");
                case "/":   return ArithmeticOp(left, right, "/");
                case "%":   return ArithmeticOp(left, right, "%");
                case ">":   return CompareOp(left, right, ">");
                case "<":   return CompareOp(left, right, "<");
                case ">=":  return CompareOp(left, right, ">=");
                case "<=":  return CompareOp(left, right, "<=");
                case "==":  return EqualityOp(left, right, true);
                case "<>":  return EqualityOp(left, right, false);
                case "AND":
                    RequireBool(left,  "AND");
                    RequireBool(right, "AND");
                    return (bool)left && (bool)right;
                case "OR":
                    RequireBool(left,  "OR");
                    RequireBool(right, "OR");
                    return (bool)left || (bool)right;
                default:
                    throw new LexorRuntimeException(
                        string.Format("Unknown binary operator '{0}'.", node.Op));
            }
        }

        // ── Arithmetic helper ─────────────────────────────────────

        private object ArithmeticOp(object left, object right, string op)
        {
            // If either operand is FLOAT, promote both to double
            if (left is double || right is double)
            {
                double l = ToDouble(left);
                double r = ToDouble(right);
                switch (op)
                {
                    case "+": return l + r;
                    case "-": return l - r;
                    case "*": return l * r;
                    case "/":
                        if (r == 0) throw new LexorRuntimeException("Division by zero.");
                        return l / r;
                    case "%": return l % r;
                }
            }
            else
            {
                int l = ToInt(left);
                int r = ToInt(right);
                switch (op)
                {
                    case "+": return l + r;
                    case "-": return l - r;
                    case "*": return l * r;
                    case "/":
                        if (r == 0) throw new LexorRuntimeException("Division by zero.");
                        return l / r;
                    case "%": return l % r;
                }
            }
            throw new LexorRuntimeException(
                string.Format("Cannot apply '{0}' to {1} and {2}.", op, left, right));
        }

        // ── Comparison helper ─────────────────────────────────────

        private object CompareOp(object left, object right, string op)
        {
            double l = ToDouble(left);
            double r = ToDouble(right);
            switch (op)
            {
                case ">":  return l > r;
                case "<":  return l < r;
                case ">=": return l >= r;
                case "<=": return l <= r;
            }
            throw new LexorRuntimeException("Unknown comparison operator.");
        }

        // ── Equality helper ───────────────────────────────────────

        private object EqualityOp(object left, object right, bool isEqual)
        {
            bool result;
            if      (left is bool && right is bool) result = (bool)left == (bool)right;
            else if (left is char && right is char) result = (char)left == (char)right;
            else                                    result = ToDouble(left) == ToDouble(right);
            return isEqual ? result : !result;
        }

        // ── Type helpers ──────────────────────────────────────────

        private int ToInt(object val)
        {
            if (val is int)    return (int)val;
            if (val is double) return (int)(double)val;
            throw new LexorRuntimeException(
                string.Format("Expected INT but got {0}.", val.GetType().Name));
        }

        private double ToDouble(object val)
        {
            if (val is int)    return (double)(int)val;
            if (val is double) return (double)val;
            throw new LexorRuntimeException(
                string.Format("Expected numeric value but got {0}.", val.GetType().Name));
        }

        private void RequireBool(object val, string op)
        {
            if (!(val is bool))
                throw new LexorRuntimeException(
                    string.Format("Operator {0} requires BOOL operands, got {1}.", op, val.GetType().Name));
        }

        // ═════════════════════════════════════════════════════════
        //  Output Formatting
        // ═════════════════════════════════════════════════════════

        private string FormatValue(object val)
        {
            if (val is bool)   return (bool)val ? "TRUE" : "FALSE";
            if (val is char)   return ((char)val).ToString();
            if (val is int)    return ((int)val).ToString();
            if (val is double)
            {
                double d = (double)val;
                // Show decimal only when needed
                return d == Math.Floor(d) && !double.IsInfinity(d)
                    ? d.ToString("0.0")
                    : d.ToString();
            }
            if (val is string) return (string)val;
            return val == null ? "" : val.ToString();
        }
    }
}