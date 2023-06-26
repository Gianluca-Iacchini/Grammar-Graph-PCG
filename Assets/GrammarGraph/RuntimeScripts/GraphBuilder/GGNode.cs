using GG.Data.Save;
using GG.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GGNode
{
    public string GUID { get; private set; }
    public int Identifier { get; set; } = -1;
    public Symbol NodeSymbol { get; set; }
    public Vector2 Position { get; set; }
    public string GroupID { get; private set; } = "";
    public bool IsExactInput { get; private set; } = false;
    public bool IsExactOutput { get; private set; } = false;


    public GGNode(int identifier, Symbol symbol, Vector2 position, string gUID = "", bool isExactInput = false, bool isExactOutput = false)
    {
        Identifier = identifier;
        NodeSymbol = symbol;
        Position = position;
        GUID = gUID;
        if (GUID == "")
            GUID = Guid.NewGuid().ToString();
        GroupID = "";
        IsExactInput = isExactInput;
        IsExactOutput = isExactOutput;
    }

    public GGNode(GGNodeSaveData nodeSaveData)
    {
        Identifier = nodeSaveData.Identifier;
        NodeSymbol = nodeSaveData.Symbol;
        Position = nodeSaveData.Position;
        GUID = nodeSaveData.ID;
        GroupID = nodeSaveData.GroupID;
        IsExactInput = nodeSaveData.IsExactInput;
        IsExactOutput = nodeSaveData.IsExactOutput;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as GGNode);
    }

    public bool Equals(GGNode other)
    {
        return other != null &&
               GUID == other.GUID;
    }

    public override int GetHashCode()
    {
        return GUID.GetHashCode();
    }

    public static bool operator ==(GGNode l, GGNode r)
    {
        if (ReferenceEquals(l, r))
            return true;
        if (ReferenceEquals(l, null) || (ReferenceEquals(r, null)))
            return false;

        return l.Equals(r);
    }

    public static bool operator !=(GGNode l, GGNode r)
    {
        return !(l == r);
    }

    public GGNodeSaveData ToSaveData()
    {
        GGNodeSaveData saveData = new GGNodeSaveData()
        {
            GroupID = "",
            ID = GUID,
            Identifier = Identifier,
            Position = Position,
            Symbol = NodeSymbol
        };

        return saveData;
    }

    public void AssingNewGUID()
    {
        this.GUID = Guid.NewGuid().ToString();
    }
}
