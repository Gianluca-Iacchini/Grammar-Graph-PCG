using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GG.Utils;

namespace GrammarGraph
{

    [Serializable]
    public class GrammarGraphNode : Node
    {
        public string ID;
        
        public Symbol NodeSymbol;

        public GrammarGraphGroup Group = null;

        public bool EntryPoint = false;
        public NodeData Data;

        private ToolbarMenu m_TitleMenu;
        private ToolbarMenu m_InputToolbarButton;
        private ToolbarMenu m_OutputToolbarButton;

        private List<VisualElement> InputPorts = new();
        private List<VisualElement> OutputPorts = new();

        public GrammarGraphNode(string title, NodeData data, bool entryPoint = false) : base()
        {
            Initialize(title, data, entryPoint);
        }

        public GrammarGraphNode() : base()
        {
            Initialize("New node", new NodeData(), false);
        }

        public GrammarGraphNode(string title, string GUID, string grammarSymbol, int ID, bool entryPoint = false) : base()
        {
            Initialize(title, new NodeData(GUID, grammarSymbol, ID), entryPoint);
        }

        private void Initialize(string title, NodeData data, bool entryPoint)
        {
            this.title = title;
            this.Data = data;
            this.ID = data.GUID;
            this.EntryPoint = entryPoint;
            Data.title = title;

            SymbolAdded += this.OnSymbolAdded;
            SymbolRemoved += this.OnSymbolRemoved;
            SymbolModified += this.OnSymbolChanged;

            m_TitleMenu = new ToolbarMenu();
            titleButtonContainer.Add(m_TitleMenu);

            m_InputToolbarButton = InitializePortToolbars(Direction.Input);
            m_OutputToolbarButton = InitializePortToolbars(Direction.Output);


            inputContainer.Add(m_InputToolbarButton);
            outputContainer.Add(m_OutputToolbarButton);
            

            RefreshExpandedState();
        }

        public void InitializeDropdown(List<Symbol> symbolList)
        {
            

            m_TitleMenu.menu.AppendAction("*", (act) => { title = "*"; });

            var asterisk = Symbol.SymbolAsterisk();

            OnSymbolAdded(null, new SymbolEventArgs { Index = 0,  NewSymbol = asterisk, OldSymbol = asterisk});

            int tntIndex = 0;
            int eIndex = 0;

            foreach (var el in symbolList)
            {
                if (el.Type == GraphSymbolType.Terminal || el.Type == GraphSymbolType.NonTerminal)
                {
                    OnSymbolAdded(this, new SymbolEventArgs { Index = tntIndex, NewSymbol = el, OldSymbol = el });
                    tntIndex++;
                }
                else if (el.Type == GraphSymbolType.Edge)
                {
                    OnSymbolAdded(this, new SymbolEventArgs { Index = eIndex, NewSymbol = el, OldSymbol = el });
                    eIndex++;
                }
            }
        }

        public ToolbarMenu InitializePortToolbars(Direction direction)
        {
            var toolbar = new ToolbarMenu();

            toolbar.style.maxWidth = 30;
            toolbar.style.minWidth = 10;
            toolbar.style.flexGrow = 1f;
            toolbar.style.minHeight = 10f;
            toolbar.style.maxHeight = 30f;
            toolbar.style.alignSelf = direction == Direction.Input ? Align.FlexStart : Align.FlexEnd;

            var children = toolbar.Children();


            children.Last().style.backgroundImage = (StyleBackground)EditorGUIUtility.Load("GraphGrammar/Resources/Add.png");
            toolbar.RemoveAt(0);
            toolbar.menu.AppendAction("Node", (act) => {this.AddPort(act.name, direction, direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi); });

            return toolbar;
        }

        public Port AddPort(string portName, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single, Symbol portSymbol = null, bool fromStart = false)
        {
            
            var p = this.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));

            if (portSymbol == null)
            {
                portSymbol = Symbol.SymbolAsterisk();
            }

            if (portSymbol.Type == GraphSymbolType.Asterisk || portSymbol.Type == GraphSymbolType.Terminal || portSymbol.Type == GraphSymbolType.NonTerminal)
            {
                portName = "Node";
            }

            p.portName = portName;
            p.userData = portSymbol;

            int iPos = fromStart ? 0 : Mathf.Max(this.inputContainer.childCount - 1, 0);
            int oPos = fromStart ? 0 : Mathf.Max(this.outputContainer.childCount - 1, 0);

            if (portDirection == Direction.Input)
            {
                this.inputContainer.Insert(iPos, p);
                InputPorts.Add(p);
            }
            else
            {
                this.outputContainer.Insert(oPos, p);
                OutputPorts.Add(p);
            }
            
            this.RefreshExpandedState();
            this.RefreshPorts();

            return p;
        }

        public void ClearInputPorts()
        {
            foreach (var p in InputPorts) 
            { 
                this.inputContainer.Remove(p);
            }
        }

        public void ClearOutputPorts()
        {
            foreach (var p in OutputPorts)
            {
                this.outputContainer.Remove(p);
            }
        }

        public List<VisualElement> GetInputPorts()
        {
            return InputPorts;
        }

        public List<VisualElement> GetOutputPorts()
        {
            return OutputPorts;
        }

        public static event EventHandler SymbolAdded;
        public static event EventHandler SymbolRemoved;
        public static event EventHandler SymbolModified;

        public class SymbolEventArgs : EventArgs
        {
            public int Index { get; set; }
            public Symbol NewSymbol { get; set; }

            public Symbol OldSymbol { get; set; }
        }

        public delegate void SymbolAddedEventHandler(object sender, SymbolEventArgs e);

        public static void AddSymbol(Symbol symbol, int index)
        {
            SymbolAdded?.Invoke(null, new SymbolEventArgs { NewSymbol = symbol, OldSymbol = symbol, Index = index });
        }

        public static void RemoveSymbol(Symbol symbol, int index)
        {
            SymbolRemoved?.Invoke(null, new SymbolEventArgs { NewSymbol = symbol, OldSymbol = symbol, Index = index });
        }

        public static void ChangeSymbol(Symbol oldSymbol, Symbol newSymbol, int index)
        {
            SymbolModified?.Invoke(null, new SymbolEventArgs { NewSymbol = newSymbol, OldSymbol = oldSymbol, Index = index });
        }

        public void OnSymbolAdded(object sender, EventArgs e)
        {


            SymbolEventArgs se = e as SymbolEventArgs;


            if (se.NewSymbol.Type == GraphSymbolType.Terminal || se.NewSymbol.Type == GraphSymbolType.NonTerminal)
            {
                m_TitleMenu.menu.InsertAction(se.Index + 1, se.NewSymbol.Name, (act) => { title = se.NewSymbol.Name; this.NodeSymbol = se.NewSymbol; });
            }
            else if (se.NewSymbol.Type == GraphSymbolType.Edge)
            {

                m_InputToolbarButton.menu.InsertAction(se.Index + 1, se.NewSymbol.Name, (act) =>
                {
                    this.AddPort(act.name, Direction.Input, Port.Capacity.Single, se.NewSymbol);
                });
                m_OutputToolbarButton.menu.InsertAction(se.Index + 1, se.NewSymbol.Name, (act) =>
                {
                    this.AddPort(act.name, Direction.Output, Port.Capacity.Multi, se.NewSymbol);
                });
            }
        }

        public void OnSymbolRemoved(object sender, EventArgs e)
        {
            SymbolEventArgs se = e as SymbolEventArgs;

            if (se.NewSymbol.Type == GraphSymbolType.Terminal || se.NewSymbol.Type == GraphSymbolType.NonTerminal)
                m_TitleMenu.menu.RemoveItemAt(se.Index + 1);
            else if (se.NewSymbol.Type == GraphSymbolType.Edge)
            {
                this.m_InputToolbarButton.menu.RemoveItemAt(se.Index + 1);
                this.m_OutputToolbarButton.menu.RemoveItemAt(se.Index + 1);
            }
        }

        public void OnSymbolChanged(object sender, EventArgs e)
        {
            SymbolEventArgs se = e as SymbolEventArgs;

            if (se.NewSymbol.Type == GraphSymbolType.Terminal || se.NewSymbol.Type == GraphSymbolType.NonTerminal)
            {
                if (this.title == se.OldSymbol.Name)
                {
                    this.title = se.NewSymbol.Name;
                    this.NodeSymbol = se.NewSymbol;
                }
            }


            OnSymbolRemoved(sender, e);
            OnSymbolAdded(sender, e);
        }

        public static void ClearList()
        {
            SymbolAdded = delegate { };
            SymbolRemoved = delegate { };
            SymbolModified = delegate { };
        }
        

        
    }


    [Serializable]
    public class NodeData
    {
        [SerializeField]
        public string GUID;
        [SerializeField]
        public string GrammarSymbol;
        [SerializeField]
        public int ID;

        [SerializeField]
        public string title;

        public NodeData()
        {
            GUID = Guid.NewGuid().ToString();
            GrammarSymbol = "*";
            ID = 0;
        }

        public NodeData(string gUID, string grammarSymbol, int ID)
        {
            GUID = gUID;
            GrammarSymbol = grammarSymbol;
            this.ID = ID;
        }
    }
}