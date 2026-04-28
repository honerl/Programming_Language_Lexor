using System;
using System.Collections.Generic;
using System.Text;

namespace LexorInterpreter.Interpreter;
using LexorInterpreter.Parser;

    /// <summary>
    /// Walks the AST produced by the Parser and executes each node.
    /// Maintains a SymbolTable for variable storage.
    /// </summary>
    public class Interpreter
    {
        private SymbolTable _symbols;

        public Interpreter()
        {
            _symbols = new SymbolTable();
        }

        // ── Public Entry ─────────────────────────────────────────

        public void Execute(ProgramNode program)
        {
            // Phase 1: process all declarations
            foreach (ASTNode node in program.Declarations)
                ExecuteNode(node);

            // Phase 2: execute statements
            foreach (ASTNode node in program.Statements)
                ExecuteNode(node);
        }

        // ── Node Dispatcher ──────────────────────────────────────

        private void ExecuteNode(ASTNode node)
        {
            if (node is DeclareNode)  ExecuteDeclare((DeclareNode)node);
            else if (node is AssignNode)   ExecuteAssign((AssignNode)node);
            else if (node is PrintNode)    ExecutePrint((PrintNode)node);
            else if (node is ScanNode)     ExecuteScan((ScanNode)node);
            else
                throw new LexorRuntimeException(
                    string.Format("Unknown statement node: {0}", node.GetType().Name));
        }

        // ── DECLARE ──────────────────────────────────────────────

        private void ExecuteDeclare(DeclareNode node)
        {
            foreach (VarInitNode varInit in node.Variables)
            {
                object initValue = null;
                if (varInit.InitValue != null)
                    initValue = Evaluate(varInit.InitValue);

                _symbols.Declare(varInit.Name, node.DataType, initValue);
            }
        }

        // ── ASSIGN ───────────────────────────────────────────────

        private void ExecuteAssign(AssignNode node)
        {
            object value = Evaluate(node.Value);

            // If the RHS was a chained assignment (x=y=4),
            // Evaluate already set y, so we just get the resulting value
            _symbols.Set(node.VariableName, value);
        }

        // ── PRINT ────────────────────────────────────────────────

        private void ExecutePrint(PrintNode node)
        {
            StringBuilder sb = new StringBuilder();

            foreach (ASTNode part in node.Parts)
            {
                if (part is NewlineNode)
                {
                    // $ flushes current line and starts a new one
                    Console.WriteLine(sb.ToString());
                    sb.Clear();
                }
                else if (part is EscapeNode)
                {
                    // [x] prints the content inside brackets literally
                    sb.Append(((EscapeNode)part).Content);
                }
                else
                {
                    object val = Evaluate(part);
                    sb.Append(FormatValue(val));
                }
            }

            // Print whatever remains on the last line
            Console.WriteLine(sb.ToString());
        }

        // ── SCAN ─────────────────────────────────────────────────

        private void ExecuteScan(ScanNode node)
        {
            // Read a single comma-separated line from stdin
            string input = Console.ReadLine() ?? "";
            string[] parts = input.Split(',');

            for (int i = 0; i < node.Variables.Count; i++)
            {
                string varName = node.Variables[i];
                string raw     = i < parts.Length ? parts[i].Trim() : "";
                string varType = _symbols.GetType(varName);

                object value = ParseInput(raw, varType, varName);
                _symbols.Set(varName, value);
            }
        }

        private object ParseInput(string raw, string type, string varName)
        {
            try
            {
                switch (type)
                {
                    case "INT":   return int.Parse(raw);
                    case "FLOAT": return double.Parse(raw);
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
            catch
            {
                throw new LexorRuntimeException(
                    string.Format("Cannot convert input '{0}' to {1} for variable '{2}'.", raw, type, varName));
            }
        }

        // ── Evaluator ────────────────────────────────────────────

        /// <summary>
        /// Evaluates any expression node and returns its runtime value.
        /// </summary>
        public object Evaluate(ASTNode node)
        {
            if (node is LiteralNode)
                return ((LiteralNode)node).Value;

            if (node is VariableNode)
                return _symbols.Get(((VariableNode)node).Name);

            if (node is AssignNode)
            {
                // Chained assignment — evaluate and set, return the value
                AssignNode assign = (AssignNode)node;
                object val = Evaluate(assign.Value);
                _symbols.Set(assign.VariableName, val);
                return _symbols.Get(assign.VariableName); // return the coerced value
            }

            if (node is UnaryOpNode)
                return EvaluateUnary((UnaryOpNode)node);

            if (node is BinaryOpNode)
                return EvaluateBinary((BinaryOpNode)node);

            throw new LexorRuntimeException(
                string.Format("Cannot evaluate node of type {0}.", node.GetType().Name));
        }

        // ── Unary ────────────────────────────────────────────────

        private object EvaluateUnary(UnaryOpNode node)
        {
            object val = Evaluate(node.Operand);

            switch (node.Op)
            {
                case "-":
                    if (val is int)    return -(int)val;
                    if (val is double) return -(double)val;
                    throw new LexorRuntimeException(
                        string.Format("Unary '-' cannot be applied to {0}.", val.GetType().Name));

                case "+":
                    if (val is int || val is double) return val;
                    throw new LexorRuntimeException(
                        string.Format("Unary '+' cannot be applied to {0}.", val.GetType().Name));

                case "NOT":
                    if (val is bool) return !(bool)val;
                    throw new LexorRuntimeException("NOT operator requires a BOOL expression.");

                default:
                    throw new LexorRuntimeException(
                        string.Format("Unknown unary operator '{0}'.", node.Op));
            }
        }

        // ── Binary ───────────────────────────────────────────────

        private object EvaluateBinary(BinaryOpNode node)
        {
            object left  = Evaluate(node.Left);
            object right = Evaluate(node.Right);

            switch (node.Op)
            {
                // Arithmetic
                case "+": return ArithmeticOp(left, right, "+");
                case "-": return ArithmeticOp(left, right, "-");
                case "*": return ArithmeticOp(left, right, "*");
                case "/": return ArithmeticOp(left, right, "/");
                case "%": return ArithmeticOp(left, right, "%");

                // Comparison
                case ">":  return CompareOp(left, right, ">");
                case "<":  return CompareOp(left, right, "<");
                case ">=": return CompareOp(left, right, ">=");
                case "<=": return CompareOp(left, right, "<=");
                case "==": return EqualityOp(left, right, true);
                case "<>": return EqualityOp(left, right, false);

                // Logical
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

        private object ArithmeticOp(object left, object right, string op)
        {
            // Promote int to double if either side is double
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

        private object EqualityOp(object left, object right, bool isEqual)
        {
            bool result;
            if (left is bool && right is bool)
                result = (bool)left == (bool)right;
            else if (left is char && right is char)
                result = (char)left == (char)right;
            else
                result = ToDouble(left) == ToDouble(right);

            return isEqual ? result : !result;
        }

        // ── Type Helpers ─────────────────────────────────────────

        private int ToInt(object val)
        {
            if (val is int)    return (int)val;
            if (val is double) return (int)(double)val;
            throw new LexorRuntimeException(
                string.Format("Expected numeric value but got {0}.", val.GetType().Name));
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
                    string.Format("Operator {0} requires BOOL operands.", op));
        }

        // ── Output Formatting ────────────────────────────────────

        /// <summary>Converts a runtime value to its LEXOR display string.</summary>
        private string FormatValue(object val)
        {
            if (val is bool)   return (bool)val ? "TRUE" : "FALSE";
            if (val is char)   return ((char)val).ToString();
            if (val is int)    return ((int)val).ToString();
            if (val is double) return ((double)val).ToString();
            if (val is string) return (string)val;
            return val == null ? "" : val.ToString();
        }
    }
