using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Utils
{
    [Serializable]
    public class Symbol
    {
        public string Name = "*";
        public GraphSymbolType Type = GraphSymbolType.Asterisk;

        public static Symbol SymbolAsterisk()
        {
            return new Symbol();
        }

        public Symbol()
        {
            Name = "*";
            Type = GraphSymbolType.Asterisk;
        }

        public Symbol(string name, GraphSymbolType type)
        {
            Name = name;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Symbol);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Type.GetHashCode();
        }


        public bool Equals(Symbol other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Name.Equals(other.Name)
                   && Type.Equals(other.Type);
        }

        public static bool operator ==(Symbol s1, Symbol s2)
        {
            if (ReferenceEquals(s1, s2))
                return true;
            if (ReferenceEquals(s1, null))
                return false;
            if (ReferenceEquals(s2, null))
                return false;
            return s1.Equals(s2);
        }

        public static bool operator !=(Symbol s1, Symbol s2)
        {
            return !(s1 == s2);
        }

        public static bool TerminalNonTerminalEquality(GraphSymbolType type1, GraphSymbolType type2)
        {
            return ((type1 == GraphSymbolType.Terminal || type1 == GraphSymbolType.NonTerminal) &&
                (type2 == GraphSymbolType.Terminal || type2 == GraphSymbolType.NonTerminal))
                || (type1 == type2);
        }

        public static string TypeToString(GraphSymbolType type)
        {
            switch (type)
            {
                case GraphSymbolType.Asterisk:
                    return "Asterisk";
                case GraphSymbolType.Terminal:
                    return "Terminal";
                case GraphSymbolType.NonTerminal:
                    return "Non Terminal";
                case GraphSymbolType.Edge:
                    return "Edge";
                default:
                    return "";
            }
        }

        public static GraphSymbolType StringToType(string str)
        {

            var lStr = str.ToLower();

            if (lStr.Contains("non"))
                return GraphSymbolType.NonTerminal;
            else if (lStr.Contains("terminal"))
                return GraphSymbolType.Terminal;
            else if (lStr.Contains("edge"))
                return GraphSymbolType.Edge;

            return GraphSymbolType.Asterisk;
        }
    }

    [Serializable]
    public enum GraphSymbolType
    {
        Asterisk,
        NonTerminal,
        Terminal,
        Edge,
    }
}