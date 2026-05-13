using System;
using System.Collections.Generic;

namespace LexorInterpreter.Interpreter
{
    /// Stores all declared variables and their values at runtime.
    /// LEXOR is strongly-typed so each variable tracks its declared type.
    public class SymbolTable
    {
        // Internal Entry
        private class Entry
        {
            public string Type;    // "INT", "FLOAT", "CHAR", "BOOL"
            public object Value;   // actual runtime value
            public int    Line;    // line number where variable was declared

            public Entry(string type, object value, int line)
            {
                Type  = type;
                Value = value;
                Line  = line;
            }
        }

        private Dictionary<string, Entry> _table;

        public SymbolTable()
        {
            _table = new Dictionary<string, Entry>();
        }

        // Declare

        /// <summary>
        /// Declares a new variable with a type and optional initial value.
        /// If no initial value is provided the default for that type is used.
        /// Throws if the variable is already declared.
        /// </summary>
        public void Declare(string name, string type, object value = null, int line = 0)
        {
            if (_table.ContainsKey(name))
                throw new LexorRuntimeException(string.Format(
                    "Line {0}: Variable '{1}' is already declared (first declared on Line {2}).",
                    line, name, _table[name].Line));

            object initial = value ?? DefaultValue(type);
            _table[name] = new Entry(type, initial, line);
        }

        // Get

        /// <summary>
        /// Returns the current value of a variable.
        /// Throws if the variable has not been declared.
        /// </summary>
        public object Get(string name)
        {
            Entry entry;
            if (!_table.TryGetValue(name, out entry))
                throw new LexorRuntimeException(
                    string.Format("Undeclared variable '{0}'.", name));
            return entry.Value;
        }

        // Get Type

        /// <summary>Returns the declared type string of a variable.</summary>
        public string GetType(string name)
        {
            Entry entry;
            if (!_table.TryGetValue(name, out entry))
                throw new LexorRuntimeException(
                    string.Format("Undeclared variable '{0}'.", name));
            return entry.Type;
        }

        // Set

        /// <summary>
        /// Assigns a new value to an already-declared variable.
        /// Performs a type-compatibility check before assigning.
        /// Throws if the variable is undeclared or types are incompatible.
        /// </summary>
        public void Set(string name, object value)
        {
            Entry entry;
            if (!_table.TryGetValue(name, out entry))
                throw new LexorRuntimeException(
                    string.Format("Undeclared variable '{0}'.", name));

            object coerced = CoerceValue(entry.Type, value, name);
            entry.Value = coerced;
        }

        // Exists

        public bool Exists(string name)
        {
            return _table.ContainsKey(name);
        }

        // Default Values

        /// <summary>Returns the zero/default value for a LEXOR data type.</summary>
        private object DefaultValue(string type)
        {
            switch (type)
            {
                case "INT":   return 0;
                case "FLOAT": return 0.0;
                case "CHAR":  return '\0';
                case "BOOL":  return false;
                default:
                    throw new LexorRuntimeException(
                        string.Format("Unknown data type '{0}'.", type));
            }
        }

        // Type Coercion / Checking

        /// <summary>
        /// Coerces a runtime value to match the declared type.
        /// Throws a descriptive error on type mismatch.
        /// </summary>
        private object CoerceValue(string declaredType, object value, string varName)
        {
            try
            {
                switch (declaredType)
                {
                    case "INT":
                        if (value is int)    return value;
                        if (value is double) return (int)(double)value;
                        if (value is bool)
                            throw new LexorRuntimeException(
                                string.Format("Cannot assign BOOL to INT variable '{0}'.", varName));
                        return Convert.ToInt32(value);

                    case "FLOAT":
                        if (value is double) return value;
                        if (value is int)    return (double)(int)value;
                        if (value is bool)
                            throw new LexorRuntimeException(
                                string.Format("Cannot assign BOOL to FLOAT variable '{0}'.", varName));
                        return Convert.ToDouble(value);

                    case "CHAR":
                        if (value is char)   return value;
                        if (value is string)
                        {
                            string s = (string)value;
                            if (s.Length == 1) return s[0];
                            throw new LexorRuntimeException(
                                string.Format("Cannot assign string to CHAR variable '{0}': must be a single character.", varName));
                        }
                        throw new LexorRuntimeException(
                            string.Format("Cannot assign {0} to CHAR variable '{1}'.", value.GetType().Name, varName));

                    case "BOOL":
                        if (value is bool)   return value;
                        if (value is string)
                        {
                            string s = ((string)value).ToUpper();
                            if (s == "TRUE")  return true;
                            if (s == "FALSE") return false;
                        }
                        throw new LexorRuntimeException(
                            string.Format("Cannot assign {0} to BOOL variable '{1}'.", value.GetType().Name, varName));

                    default:
                        throw new LexorRuntimeException(
                            string.Format("Unknown type '{0}' for variable '{1}'.", declaredType, varName));
                }
            }
            catch (LexorRuntimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LexorRuntimeException(
                    string.Format("Type error assigning to '{0}': {1}", varName, ex.Message));
            }
        }

        // Debug Dump

        public void Dump()
        {
            Console.WriteLine("=== Symbol Table ===");
            foreach (KeyValuePair<string, Entry> kv in _table)
            {
                Console.WriteLine("  {0,-15} | {1,-6} | {2}",
                    kv.Key, kv.Value.Type, kv.Value.Value);
            }
            Console.WriteLine("====================");
        }
    }

    // Runtime Exception

    public class LexorRuntimeException : Exception
    {
        public LexorRuntimeException(string message) : base(message) { }
    }
}