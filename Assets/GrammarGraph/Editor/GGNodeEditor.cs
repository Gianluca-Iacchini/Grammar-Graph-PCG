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

namespace GG.Editor
{

    [Serializable]
    public class GGNodeEditor : Node
    {
        public string ID;

        public GGGraphView graphView;

        public Vector2 Position = Vector2.zero;

        private Symbol _nodeSymbol = Symbol.SymbolAsterisk();
        public Symbol NodeSymbol { get { return _nodeSymbol; }
            set {
                _nodeSymbol = value;
                if (_nodeSymbol == null) _nodeSymbol = Symbol.SymbolAsterisk();
                this.title = SetTitle();
            }
        }

        private int _nodeIdentifier = -1;
        public int NodeIdentifier { get { return _nodeIdentifier; } set { _nodeIdentifier = value; this.title = SetTitle(); } }

        public bool IsExactInput { get { return m_Toggle_In.value; } set { m_Toggle_In.value = value; } }
        public bool IsExactOutput { get { return m_Toggle_Out.value; } set { m_Toggle_Out.value = value; } }

        public GGGroupEditor Group = null;

        public bool EntryPoint = false;

        private ToolbarMenu m_SymbolMenu;
        private ToolbarMenu m_IdentifierMenu;
        private ToolbarMenu m_InputToolbarButton;
        private ToolbarMenu m_OutputToolbarButton;

        private Toggle m_Toggle_In;
        private Toggle m_Toggle_Out;

        private List<Port> InputPorts = new();
        private List<Port> OutputPorts = new();

        public GGNodeEditor(string GUID, string grammarSymbol, int ID, bool entryPoint = false) : base()
        {
            Initialize(GUID, grammarSymbol, ID, entryPoint);
        }

        public GGNodeEditor() : base()
        {
            Initialize(Guid.NewGuid().ToString(), "*", 0 , false);
        }

        /// <summary>
        /// Initialize node and add all the required ports, buttons and menus
        /// </summary>
        private void Initialize(string GUID, string grammarSymbol, int ID, bool entryPoint)
        {

            
            this.ID = GUID;
            this.EntryPoint = entryPoint;

            this.title = SetTitle();

            SymbolAdded += this.OnSymbolAdded;
            SymbolRemoved += this.OnSymbolRemoved;
            SymbolModified += this.OnSymbolChanged;

            titleButtonContainer.RemoveAt(0);

            m_SymbolMenu = new ToolbarMenu();
            m_IdentifierMenu = new ToolbarMenu();
            titleContainer.Insert(0, m_IdentifierMenu);
            titleContainer.Add(m_SymbolMenu);
            

            m_InputToolbarButton = InitializePortToolbars(Direction.Input);
            m_OutputToolbarButton = InitializePortToolbars(Direction.Output);

            m_Toggle_In = new Toggle("Exact");
            m_Toggle_In.ElementAt(0).style.minWidth = 10f;
            m_Toggle_In.style.alignItems = Align.Center;

            m_Toggle_Out = new Toggle("Exact");
            m_Toggle_Out.ElementAt(0).style.minWidth = 10f;
            m_Toggle_Out.style.alignItems = Align.Center;

            inputContainer.Insert(0 ,m_Toggle_In);
            outputContainer.Insert(0, m_Toggle_Out);

            inputContainer.Add(m_InputToolbarButton);
            outputContainer.Add(m_OutputToolbarButton);
            

            RefreshExpandedState();
        }

        /// <summary>
        /// Initialize the symbol dropdown menu
        /// </summary>
        public void InitializeDropdown(List<Symbol> symbolList)
        {
            

            m_SymbolMenu.menu.AppendAction("*", (act) => { this.NodeSymbol = Symbol.SymbolAsterisk(); });

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
            toolbar.menu.AppendAction("Node", (act) => { this.AddPort(act.name, direction); });

            return toolbar;
        }


        public void AddIdentifiers(int nNodes)
        {
            int nIdentifiers = m_IdentifierMenu.menu.MenuItems().Count;

            for (int i = nIdentifiers; i < nNodes; i++) 
            {
                /* Not sure why, but i variable has different value inside the lambda expression */
                int tmp = i;
                m_IdentifierMenu.menu.AppendAction(i.ToString(), (act) => { this.NodeIdentifier = tmp; });
            }
        }

        public void RemoveIdentifiers(int nNodes) 
        {
            int nIdentifiers = m_IdentifierMenu.menu.MenuItems().Count;

            if (nNodes > nIdentifiers) return;

            for (int i = nIdentifiers; i > nNodes && i > 0; i--)
            {
                m_IdentifierMenu.menu.RemoveItemAt(i - 1);
            }

            if (this.NodeIdentifier >= nNodes)
            {
                this.NodeIdentifier = nNodes - 1;
            }
        }

        private string SetTitle()
        {
            if (NodeIdentifier >= 0)
            {
                return $"{NodeIdentifier} : {NodeSymbol.Name}";
            }
            else
            {
                return $"{NodeSymbol.Name}";
            }
        }
        public static void ClearList()
        {
            SymbolAdded = delegate { };
            SymbolRemoved = delegate { };
            SymbolModified = delegate { };
        }

        public void HideToggle()
        {
            m_Toggle_In.style.display = DisplayStyle.None;
            m_Toggle_Out.style.display = DisplayStyle.None;
        }

        #region Ports
        /// <summary>
        /// Adds a new port based on the symbol and direction specified by the user
        /// </summary>
        /// <returns></returns>
        public Port AddPort(string portName, Direction portDirection, Symbol portSymbol = null, bool fromStart = false)
        {
            
            var p = this.InstantiatePort(Orientation.Horizontal, portDirection, Port.Capacity.Multi, typeof(float));

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

            int iPos = fromStart ? 1 : Mathf.Max(this.inputContainer.childCount - 1, 0);
            int oPos = fromStart ? 1 : Mathf.Max(this.outputContainer.childCount - 1, 0);

            Button deleteButton = new Button();
            deleteButton.text = "X";
            VisualElement portContainer = inputContainer;

            if (portDirection == Direction.Input)
            {
                this.inputContainer.Insert(iPos, p);
                InputPorts.Add(p);
                portContainer = inputContainer;
            }
            else
            {
                this.outputContainer.Insert(oPos, p);
                OutputPorts.Add(p);
                portContainer = outputContainer;

            }

            deleteButton.clicked += () => { graphView.DeleteElements(p.connections); p.DisconnectAll();  portContainer.Remove(p); this.RefreshExpandedState(); this.RefreshPorts(); };
            
            if (!fromStart)
                p.Add(deleteButton);

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

        public List<Port> GetInputPorts()
        {
            return InputPorts;
        }

        public List<Port> GetOutputPorts()
        {
            return OutputPorts;
        }

        private void RemovePorts(Symbol symbol)
        {
            foreach (Port p in InputPorts)
            {
                if (p.userData is Symbol s)
                {
                    if (s == symbol)
                    {
                        p.DisconnectAll();
                        inputContainer.Remove(p);
                    }
                }
            }

            foreach (Port p in OutputPorts)
            {
                if (p.userData is Symbol s)
                {
                    if (s == symbol)
                    {
                        p.DisconnectAll();
                        outputContainer.Remove(p);
                    }
                }
            }
        }


        private void RenamePorts(Symbol symbol)
        {
            foreach (Port p in InputPorts)
            {
                if (p.userData is Symbol s)
                {
                    if (s == symbol)
                    {
                        p.portName = symbol.Name;
                        p.userData = symbol;
                    }
                }
            }

            foreach (Port p in OutputPorts)
            {
                if (p.userData is Symbol s)
                {
                    if (s == symbol)
                    {
                        p.portName = symbol.Name;
                        p.userData = symbol;
                    }
                }
            }
        }
        #endregion Ports

        #region Events
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
                m_SymbolMenu.menu.InsertAction(se.Index + 1, se.NewSymbol.Name, (act) => { this.NodeSymbol = se.NewSymbol; });
            }
            else if (se.NewSymbol.Type == GraphSymbolType.Edge)
            {

                m_InputToolbarButton.menu.InsertAction(se.Index + 1, se.NewSymbol.Name, (act) =>
                {
                    this.AddPort(act.name, Direction.Input, se.NewSymbol);
                });
                m_OutputToolbarButton.menu.InsertAction(se.Index + 1, se.NewSymbol.Name, (act) =>
                {
                    this.AddPort(act.name, Direction.Output, se.NewSymbol);
                });
            }
        }

        public void OnSymbolRemoved(object sender, EventArgs e)
        {


            SymbolEventArgs se = e as SymbolEventArgs;

            if (se.NewSymbol.Type == GraphSymbolType.Terminal || se.NewSymbol.Type == GraphSymbolType.NonTerminal)
            {
                m_SymbolMenu.menu.RemoveItemAt(se.Index + 1);
                if (this.title == se.NewSymbol.Name)
                {
                    this.NodeSymbol = Symbol.SymbolAsterisk();
                }
            }
            else if (se.NewSymbol.Type == GraphSymbolType.Edge)
            {
                this.m_InputToolbarButton.menu.RemoveItemAt(se.Index + 1);
                this.m_OutputToolbarButton.menu.RemoveItemAt(se.Index + 1);
                RemovePorts(se.NewSymbol);
            }
        }

        public void OnSymbolChanged(object sender, EventArgs e)
        {
            SymbolEventArgs se = e as SymbolEventArgs;

            if (se.NewSymbol.Type == GraphSymbolType.Terminal || se.NewSymbol.Type == GraphSymbolType.NonTerminal)
            {
                if (this.title == se.OldSymbol.Name)
                {
                    this.NodeSymbol = se.NewSymbol;
                }

                m_SymbolMenu.menu.RemoveItemAt(se.Index + 1);
            }

            else if (se.NewSymbol.Type == GraphSymbolType.Edge)
            {
                this.m_InputToolbarButton.menu.RemoveItemAt(se.Index + 1);
                this.m_OutputToolbarButton.menu.RemoveItemAt(se.Index + 1);
                RenamePorts(se.NewSymbol);
            }

            OnSymbolAdded(sender, e);
        }

        #endregion Events
    }


    //[Serializable]
    //public class NodeData
    //{
    //    [SerializeField]
    //    public string GUID;
    //    [SerializeField]
    //    public string GrammarSymbol;
    //    [SerializeField]
    //    public int ID;

    //    [SerializeField]
    //    public string title;

    //    public NodeData()
    //    {
    //        GUID = Guid.NewGuid().ToString();
    //        GrammarSymbol = "*";
    //        ID = 0;
    //    }

    //    public NodeData(string gUID, string grammarSymbol, int ID)
    //    {
    //        GUID = gUID;
    //        GrammarSymbol = grammarSymbol;
    //        this.ID = ID;
    //    }
    //}
}