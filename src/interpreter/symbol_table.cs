using System;
using System.Collections.Generic;
using System.ComponentModel;
using Language;

namespace LexorInterpreter.Interpreter;
public class SymbolTable
{
    private class Entry
    {
        public string Type;
        public object Value;
        public Entry(string type, object value)
        {
            Type = type;
            Value = value;
        }
    }
    private Dictionary<string, Entry> _table;
    public SymbolTable()
    {
        _table = new Dictionary<string, Entry>();
    }

    public void Declare(string name, string type, object value = null)
    {
        if(_table.ContainsKey(name))
        {
            // Temporary Error Handling should be replaced with proper error handling
            throw new Exception(
                string.Format("Variable '{0}' is already declared.", name));
        }
        object initial = value ?? DefaultValue(type);
        _table[name] = new Entry(type, initial);

    }

    // Type Getters
    public object Get(string name)
    {
        Entry entry;
        if(!_table.TryGetValue(name, out entry))
        {
            throw new Exception(
                string.Format("Undeclared Variable '{0}'.", name)
            );
        }
        return entry.Value;
    }

    public string GetType(string name)
    {
        Entry entry;
        if(!_table.TryGetValue(name, out entry))
        {
            throw new Exception(
                string.Format("Undeclared variable '{0}'.", name)
            );
        }
        return entry.Type;
    }

    // Setters
    public void Set(string name, object value)
    {
        Entry entry;
        if(!_table.TryGetValue(name, out entry))
        {
            throw new LexorRuntimeException(
                string.Format("Undeclared Variable {0}.", name)
            );
        }

        object coereced = CoerceValue(entry.Type, value, name);
        entry.Value = coereced;
    }

    public bool Exists(string name)
    {
        return _table.ContainsKey(name);
    }

    

    // Default Value
    private object DefaultValue(string type)
    {
        switch (type)
        {
            case "INT": return 0;
            case "FLOAT": return 0.0;
            case "CHAR" : return '\0';
            case "BOOL" : return false;
            default: 
                throw new Exception(
                    string.Format("Unknown data type '{0}'.", type)        
                );
        }
    }

    private object CoerceValue(string declaredType, object value, string varName)
    {
        try
        {
            switch(declaredType)
            {
                // What about for string?
                case "INT":
                    if(value is int) return value;
                    if(value is double) return (int)(double)value;
                    if(value is bool) 
                        throw new LexorRuntimeException(
                            string.Format("Cannot assign BOOL to INT variable '{0}'.",varName)
                        );
                    return Convert.ToInt32(value);
                case "FLOAT":
                    if(value is double) return value;
                    if(value is int)    return (double)(int)value;
                    if(value is bool)   
                        throw new LexorRuntimeException(
                            string.Format("Cannot assign BOOL to FLOAT variable '{0}'.", varName)
                        );
                    return Convert.ToDouble(value);
                case "CHAR":
                    if(value is char) return value;
                    if(value is string)
                    {
                        string s = (string) value;
                        if(s.Length == 1) return s[0];
                        throw new LexorRuntimeException(
                            string.Format("Cannot assign a string to CHAR variable '{0}': must be a single character.", varName)
                        );
                    }
                    throw new LexorRuntimeException(
                        string.Format("Cannot assign {0} to CHAR variable '{1}'.", value.GetType().Name, varName)
                    );
                case "BOOL":
                    if(value is bool) return value;
                    if(value is string)
                    {
                        string s = ((string) value).ToUpper();
                        if(s == "TRUE") return true;
                        if(s == "FALSE") return false;
                    }
                    throw new LexorRuntimeException(
                        string.Format("Cannot assign {0} to BOOL variable '{1}'.", value.GetType().Name, varName)
                    );
                default:
                    throw new LexorRuntimeException(
                        string.Format("Unknown type '{0}' for variable '{1}'.",declaredType, varName)
                    );

            }
        } 
        catch (LexorRuntimeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LexorRuntimeException(
                string.Format("Type error assigning to '{0}': '{1}'", varName, ex.Message)
            );
        }
    }


    // Debug DUMP
    public void Dump()
    {
        Console.WriteLine("=== Symbol Table ===");
        foreach (KeyValuePair<string, Entry> kv in _table)
        {
            Console.WriteLine(" {0, -15} | {1, -6} | {2}",
            kv.Key, kv.Value.Type, kv.Value.Value);
        }
        Console.WriteLine("===================");
    }


    // Runtime Exception To be separated later on
    public class LexorRuntimeException : Exception
    {
        public LexorRuntimeException(string message) : base(message) {}
    }





}