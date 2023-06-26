using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Metadata;
using static UnityEditor.Progress;
using GG.Utils;
using GG.Editor.Utils;
using GG.Data.Save;
using GG.ScriptableObjects;
using GG.Builder;

namespace GG.Editor
{
    public class GGEditorWindow : GraphViewEditorWindow
    {
        private ListView m_SymbolListView;
        private ListView m_GraphRuleListView;

        private VisualElement m_LGraphParent;
        private VisualElement m_RGraphParent;

        private TwoPaneSplitView m_LeftPanelSplitView;
        private TwoPaneSplitView m_GraphPanelSplitView;

        public List<Symbol> Symbols;
        public List<GraphRule> Rules;

        private GGGraphView m_ResultGraphView;
        private VisualElement m_ResultGraphParent;

        private TabbedView m_GraphTabbedView;

        private TextField m_GraphName;

        private TabButton m_GraphViewResultTab;
        private TabButton m_RulesTab;

        [MenuItem("Graph/Grammar Graph")]
        public static void OpenGraphGrammarWindow()
        {
            var window = GetWindow<GGEditorWindow>();
            window.titleContent = new GUIContent("Grammar Graph");
        }

        /* Window position is not yet initialized in CreateGUI, so we need to wait until OnEnable to perform adjustments. */
        private void OnEnable()
        {
            float windowWidth = GetWindow<GGEditorWindow>().position.width;

            float leftPaneSize = Mathf.Clamp(windowWidth / 4f, 10, 275);
            float graphSize = (windowWidth - leftPaneSize) / 2f;

            m_LeftPanelSplitView.fixedPaneInitialDimension = leftPaneSize;
            m_GraphPanelSplitView.fixedPaneInitialDimension = graphSize;            
        }

        public void CreateGUI()
        {
            Toolbar newToolbar = new Toolbar();
            rootVisualElement.Add(newToolbar);
            var tbutton = new ToolbarButton(()=> { GGSaveGraph saveGraph = new GGSaveGraph(RulesToRuleGraphSaveData(Rules), Symbols); saveGraph.Save(m_GraphName.value); });
            tbutton.text = "Save";
            newToolbar.Add(tbutton);

            var lButton = new ToolbarButton(LoadGraph);
            lButton.text = "Load";
            newToolbar.Add(lButton);

            m_GraphName = new TextField("Graph Name");
            m_GraphName.value = "New Graph";    
            m_GraphName.style.minWidth = 250;
            m_GraphName.labelElement.style.minWidth = 10;
            m_GraphName.labelElement.style.alignContent = Align.Center; 
            newToolbar.Add(m_GraphName);

            var vf2Button = new ToolbarButton(VF2Test);
            vf2Button.text = "Sample rules";
            newToolbar.Add(vf2Button);

            Symbols = new List<Symbol>();
            Rules = new List<GraphRule>();

            m_LeftPanelSplitView = new TwoPaneSplitView(0, 125, TwoPaneSplitViewOrientation.Horizontal);
            m_GraphPanelSplitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);


            var lGraphView = ConstructGraphView();
            var rGraphView = ConstructGraphView();

            Rules.Add(new GraphRule("Start Rule", lGraphView, rGraphView));

            m_LGraphParent = new VisualElement();
            m_LGraphParent.Add(lGraphView);
            m_GraphPanelSplitView.Add(m_LGraphParent);
            lGraphView.StretchToParentSize();


            /* RectangleSelector is bugged when used with TwoPaneSplitView, in order to have a correct selector we need to wrap the second panel with an empty VisualElement */
            m_RGraphParent = new VisualElement();
            m_RGraphParent.Add(rGraphView);
            m_GraphPanelSplitView.Add(m_RGraphParent);
            rGraphView.StretchToParentSize();

            m_ResultGraphParent = new VisualElement();
            m_ResultGraphView = ConstructGraphView();
            m_ResultGraphParent.Add(m_ResultGraphView);
            m_ResultGraphView.StretchToParentSize();

            m_GraphTabbedView = ConstructGraphTabView();


            m_SymbolListView = ConstructSymbolListView();
            m_GraphRuleListView = ConstructRuleListView();

            VisualElement leftPanel = ConstructLeftPanel();
            m_LeftPanelSplitView.Add(leftPanel);
            //m_LeftPanelSplitView.Add(m_GraphPanelSplitView);
            m_LeftPanelSplitView.Add(m_GraphTabbedView);
            rootVisualElement.Add(m_LeftPanelSplitView);


        }

        private List<RuleGraphSaveData> RulesToRuleGraphSaveData(List<GraphRule> rules)
        {
            List<RuleGraphSaveData> ruleGraphSaveDatas = new List<RuleGraphSaveData>();

            foreach (var r in rules)
            {
                RuleGraphSaveData rgsd = new RuleGraphSaveData();
                rgsd.Name = r.ruleName;
                rgsd.LeftGraph = r.LGraph.GetGraphSaveData();
                rgsd.RightGraph = r.RGraph.GetGraphSaveData();

                ruleGraphSaveDatas.Add(rgsd);
            }

            return ruleGraphSaveDatas;
        }

        private void LoadGraph()
        {
            GGSaveGraph saveGraph = new GGSaveGraph(RulesToRuleGraphSaveData(Rules), Symbols);
            var saveData = GGSaveGraph.Load(m_GraphName.value);

            if (saveData == null)
            {
                Debug.LogError($"No graph named: {m_GraphName.value} found");
                return;
            }

            foreach (var r in Rules) 
            {
                r.LGraph.ClearGraph(); 
                r.RGraph.ClearGraph(); 
            }

            this.Symbols.Clear();
            m_SymbolListView.RefreshItems();

            foreach (var s in saveData.SymbolList)
            {
                SymbolAddAction(s);
            }

            this.Rules.Clear();
            m_GraphRuleListView.RefreshItems();

            foreach (var r in saveData.RuleList) 
            {
                GGGraphView leftGraphView = new GGGraphView();
                leftGraphView.Symbols = this.Symbols;
                leftGraphView.CreateFromSO(r.LeftGraph);
                GGGraphView rightGraphView = new GGGraphView();
                rightGraphView.Symbols = this.Symbols;
                rightGraphView.CreateFromSO(r.RightGraph);

                Rules.Add(new GraphRule(r.Name, leftGraphView, rightGraphView));
                m_GraphRuleListView.RefreshItems();
            }

            if (Rules.Count > 0)
            {
                ChangeGraphViews(Rules[0]);
            }
        }

        private void VF2Test()
        {
            foreach (GraphRule r in Rules)
            {
                foreach (var n in r.LGraph.nodes)
                {
                    GGNodeEditor node = n as GGNodeEditor;
                    if (node != null)
                    {
                        foreach (var n2 in r.LGraph.nodes)
                        {
                            GGNodeEditor node2 = n2 as GGNodeEditor;
                            if (node2 != null)
                            {
                                if (node != node2 && node.NodeIdentifier == node2.NodeIdentifier)
                                {
                                    Debug.LogError("Same node identifier in left graph of rule " + r.ruleName);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            foreach (GraphRule r in Rules)
            {
                foreach (var n in r.RGraph.nodes)
                {
                    GGNodeEditor node = n as GGNodeEditor;
                    if (node != null)
                    {
                        foreach (var n2 in r.RGraph.nodes)
                        {
                            GGNodeEditor node2 = n2 as GGNodeEditor;
                            if (node2 != null)
                            {
                                if (node != node2 && node.NodeIdentifier == node2.NodeIdentifier && node.Group == node2.Group)
                                {
                                    Debug.LogError("Same node identifier in right graph of rule " + r.ruleName);
                                    return;
                                }
                            }
                        }
                    }
                }
            }


            GGSaveGraph saveGraph = new GGSaveGraph(RulesToRuleGraphSaveData(Rules), Symbols);
            saveGraph.Save(m_GraphName.value);
            
            

            var saveData = GGSaveGraph.Load(m_GraphName.value);

            if (saveData == null)
            {
                Debug.LogError("File does not exists");
                return;
            }

            var ruleList = saveData.RuleList;

            GGGraph res = GGBuilder.ApplyAllRules(ruleList);

            if (res != null)
            {
                m_ResultGraphView.ClearGraph();
                m_ResultGraphView.CreateFromSaveData(res.ToSaveData());
                m_ResultGraphView.HideAllToggles();
            }

            m_GraphTabbedView.Activate(m_GraphViewResultTab);
        }

        private GGGraphView ConstructGraphView()
        {
            GGGraphView graphView = new GGGraphView
            {
                name = "Dialogue Graph",
                Symbols = this.Symbols,
            };

            graphView.CreateNode("Start", new Vector2(150f, 200f));


            return graphView;
        }

        private string RenameRule(string newName)
        {
            int counter = 0;
            string modifiedString = newName;

            while (Rules.Any(r => r.ruleName == modifiedString))
            {
                counter++;
                modifiedString = IncrementCounter(newName, counter);
            }

            return modifiedString;
        }

        private string RenameSymbol(string newName)
        {

            int counter = 0;
            string modifiedString = newName;

            while (Symbols.Any (s => s.Name == modifiedString))
            {
                counter++;
                modifiedString = IncrementCounter(newName, counter);
            }

            return modifiedString;
        }

        private string IncrementCounter(string newString, int counter)
        {
            int index = newString.Length - 1;

            while (index >= 0 && char.IsDigit(newString[index]))
            {
                index--;
            }

            if (index < newString.Length - 1)
            {
                string prefix = newString.Substring(0, index + 1);
                string numberString = newString.Substring(index + 1);
                int number = int.Parse(numberString);
                number += counter;
                return prefix + number.ToString();
            }

            return newString + counter.ToString();
        }

        private ListView ConstructSymbolListView()
        {
            ListView listView = new ListView();

            listView.makeItem = () =>
            {
                int index = listView.itemsSource.Count - 1;
                string symbolName = Symbols[index].Name;

                Symbol symbol = new Symbol(symbolName, Symbols[index].Type);


                var item = new SymbolItem(symbol, index, GetIndexOfSymbolType(symbol.Type, index));

                item.OnItemNameChange += (index, oldVal, newVal) => { string newName = RenameSymbol(newVal); Symbol s = Symbols[index]; s.Name = newName; Symbols[index] = s; return newName; };
                item.OnItemNameChange += (i, o, n) => {
                    var item = Symbols[i];

                    if (item.Type == GraphSymbolType.Edge)
                    {
                        foreach (var r in Rules)
                        {
                            r.LGraph.ChangePortsName(o, n);
                            r.RGraph.ChangePortsName(o, n);
                        }
                    }

                    return n;
                };

                item.OnDeleteSymbol += (sym, index) => {
                    GGNodeEditor.RemoveSymbol(symbol, GetIndexOfSymbolType(symbol.Type, index));
                    Symbols.Remove(symbol);
                    m_SymbolListView.RefreshItems();
                };

                return item;
            };
            listView.bindItem = (item, index) =>
            {
                SymbolItem it = item as SymbolItem;

                if (it != null) 
                {
                    it.SetSymbolName(Symbols[index].Name);
                    it.SetSymbolType(Symbols[index].Type);
                    it.SetIndex(index, GetIndexOfSymbolType(Symbols[index].Type, index));
                }

            };

            listView.itemIndexChanged += (fIndex, sIndex) => 
            {
                var fItem = Symbols[sIndex];
                var sItem = Symbols[fIndex];

                int symbolFirstIndex = GetIndexOfSymbolType(fItem.Type, fIndex);

                if (fIndex > sIndex)
                    symbolFirstIndex = Symbol.TerminalNonTerminalEquality(fItem.Type, sItem.Type) ? symbolFirstIndex : symbolFirstIndex-1;

                GGNodeEditor.RemoveSymbol(fItem, symbolFirstIndex);
                GGNodeEditor.AddSymbol(fItem, GetIndexOfSymbolType(fItem.Type, sIndex));
            };

            listView.itemsSource = Symbols;

            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;

            return listView;
        }

        private int GetIndexOfSymbolType(GraphSymbolType type, int absoluteIndex)
        {
            int symbolIndex = 0;


            for (int i = symbolIndex; i < absoluteIndex; i++)
            {
                if (Symbol.TerminalNonTerminalEquality(type, Symbols[i].Type) || Symbols[i].Type == type)
                    symbolIndex += 1;
            }

            return symbolIndex;
        }

        private ListView ConstructRuleListView()
        {
            ListView listView = new ListView();

            listView.makeItem = () =>
            {
                var gri = new GraphRuleItem("Graph Rule");

                gri.OnItemNameChange = (index, oldVal, newVal) => { string newName = RenameRule(newVal); GraphRule r = Rules[index]; r.ruleName = newName; Rules[index] = r; return newName; };

                return gri;
            };
            listView.bindItem = (item, index) =>
            {
                GraphRuleItem it = item as GraphRuleItem;

                if (it != null)
                {
                    it.SetName(Rules[index].ruleName);
                    it.Index = index;
                }
            };

            listView.itemsSource = Rules;

            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;

            listView.onSelectionChange += (en) =>
            {
                if (en.Count() > 0)
                {
                    if (en.First() != null && en.First() is GraphRule item)
                    {
                        ChangeGraphViews(item);
                    }
                }
            };

            return listView;
        }

        private void ChangeGraphViews(GraphRule graphRule)
        {
            if (m_LGraphParent.childCount > 0 && m_RGraphParent.childCount > 0)
            {

                m_LGraphParent.RemoveAt(0);
                m_RGraphParent.RemoveAt(0);

                m_LGraphParent.Add(graphRule.LGraph);
                m_RGraphParent.Add(graphRule.RGraph);

                graphRule.LGraph.StretchToParentSize();
                graphRule.RGraph.StretchToParentSize();
            }
        }

        private TabbedView ConstructGraphTabView()
        {
            var tabbedView = new TabbedView();

            m_RulesTab = new TabButton("Rules", m_GraphPanelSplitView);
            m_GraphViewResultTab = new TabButton("Result", m_ResultGraphParent);

            tabbedView.AddTab(m_RulesTab, true);
            tabbedView.AddTab(m_GraphViewResultTab, false);


            return tabbedView;
        }

        private TabbedView ConstructListviewTabview()
        {
            var tabbedView = new TabbedView();

            Button rButton = new Button(() => { Rules.Add(new GraphRule(RenameRule("Graph Rule"), ConstructGraphView(), ConstructGraphView())); m_GraphRuleListView.RefreshItems(); });
            rButton.text = "+";
            VisualElement rulesParent = new VisualElement();
            var rToolbar = ConstructToolbar();
            rToolbar.Add(rButton);
            rulesParent.Add(rToolbar);
            rulesParent.Add(m_GraphRuleListView);

            ToolbarMenu sToolbarMenu = new ToolbarMenu();
            var children = sToolbarMenu.Children();
            children.Last().style.backgroundImage = (StyleBackground)EditorGUIUtility.Load("GraphGrammar/Resources/Add.png");
            sToolbarMenu.RemoveAt(0);
            sToolbarMenu.menu.AppendAction(Symbol.TypeToString(GraphSymbolType.NonTerminal), SymbolAddAction);
            sToolbarMenu.menu.AppendAction(Symbol.TypeToString(GraphSymbolType.Terminal), SymbolAddAction);
            sToolbarMenu.menu.AppendAction(Symbol.TypeToString(GraphSymbolType.Edge), SymbolAddAction);
            
            VisualElement symbolParent = new VisualElement();
            var sToolbar = ConstructToolbar();
            sToolbar.Add(sToolbarMenu);
            symbolParent.Add(sToolbar);
            symbolParent.Add(m_SymbolListView);

            TabButton rulesTab = new TabButton("Rules", rulesParent);
            TabButton symbolsTab = new TabButton("Symbols", symbolParent);
            

            tabbedView.AddTab(rulesTab, true);
            tabbedView.AddTab(symbolsTab, false);


            return tabbedView;
        }

        private void SymbolAddAction(Symbol symbol)
        {
            Symbols.Add(symbol);
            m_SymbolListView.RefreshItems();
            GGNodeEditor.AddSymbol(symbol, GetIndexOfSymbolType(symbol.Type, Symbols.Count - 1));
        }

        private void SymbolAddAction(DropdownMenuAction act)
        {


            GraphSymbolType type = Symbol.StringToType(act.name);
            if (type == GraphSymbolType.Asterisk) return;

            string name;

            switch (type)
            {
                case GraphSymbolType.Terminal:
                    name = "t";
                    break;
                case GraphSymbolType.NonTerminal:
                    name = "T";
                    break;
                case GraphSymbolType.Edge:
                    name = "E";
                    break;
                default:
                    return;
            }

            Symbol s = new Symbol(RenameSymbol(name), type);
            SymbolAddAction(s);
        }
    

        private Toolbar ConstructToolbar()
        {
            var toolbar = new Toolbar();
            ToolbarSearchField searchField = new ToolbarSearchField();
            searchField.style.width = 150f;

            toolbar.Add(searchField);

            return toolbar;
        }

        private VisualElement ConstructLeftPanel()
        {
            VisualElement sidePanel = new VisualElement();
            var tabbedView = ConstructListviewTabview();

            sidePanel.Add(tabbedView);

            return sidePanel;

        }

        private void OnDisable()
        {
            GGNodeEditor.ClearList();
        }
    }

    

    class GraphRuleItem : VisualElement
    {
        public string RuleName;

        VisualElement m_Root;
        Label m_NameLabel;

        int m_LabelClicks = 0;

        TextField m_NameTextField;

        public delegate string NameChange(int index, string oldName, string newName);

        public NameChange OnItemNameChange = null;

        public int Index = 0;

        public GraphRuleItem(string name = "Graph Rule")
        {
            RuleName = name;

            this.style.borderBottomColor = new StyleColor(Color.black);
            this.style.borderBottomWidth = new StyleFloat(0.5f);

            m_Root = new VisualElement();
            m_Root.style.flexDirection = FlexDirection.Row;

            m_NameLabel = new Label(RuleName);

            m_NameTextField = new TextField();

            m_NameLabel.StretchToParentSize();
            m_NameLabel.style.minHeight = 20f;

            m_Root.Add(m_NameLabel);
            m_NameLabel.RegisterCallback<FocusOutEvent>((evt) => { m_LabelClicks = 0; });
            m_NameLabel.RegisterCallback<MouseDownEvent>(OnLabelSelect);

            m_NameLabel.focusable = true;

            m_NameTextField.RegisterCallback<FocusOutEvent>((evt) =>
            {

                string oldName = m_NameLabel.text;
                string newName = m_NameTextField.value;

                if (newName != oldName)
                {
                    if (OnItemNameChange != null)
                    {
                        newName = OnItemNameChange(Index, oldName, newName);
                    }

                    m_NameTextField.value = newName;
                    RuleName = newName;
                }
                m_Root.Remove(m_NameTextField);
                m_Root.Add(m_NameLabel);
                m_NameLabel.text = m_NameTextField.value;

            });

            Add(m_Root);


        }

        public void SetName(string text)
        {
            RuleName = text;
            this.m_NameLabel.text = RuleName;
        }

        public void OnLabelSelect(MouseDownEvent evt)
        {
            m_LabelClicks++;

            m_NameLabel.Focus();

            if (m_LabelClicks >= 2)
            {
                m_LabelClicks = 0;

                m_Root.Remove(m_NameLabel);


                m_NameTextField.value = m_NameLabel.text;

                m_Root.Add(m_NameTextField);
                m_NameTextField.Focus();
            }
        }
    }

    class SymbolItem : VisualElement
    {
        public delegate string NameChange(int index, string oldName, string newName);
        public NameChange OnItemNameChange = null;

        public delegate void DeleteSymbol(SymbolItem symbolItem, int index);
        public DeleteSymbol OnDeleteSymbol = null;

        Symbol m_Symbol;
        int m_Index;
        int m_SymbolIndex;

        TextField m_SymbolNameTextField;
        VisualElement m_Root;



        public SymbolItem(Symbol symbol, int index = 0, int symbIndex = 0)
        {
            Initialize(symbol, index, symbIndex);
        }

        public SymbolItem(string name, GraphSymbolType symbolType, int index = 0, int symbIndex = 0)
        {
            Initialize(new Symbol(name, symbolType), index, symbIndex);
        }

        private void Initialize(Symbol symbol, int index, int symbIndex)
        {
            m_Symbol = symbol;
            m_Index = index;
            m_SymbolIndex = symbIndex;

            this.AddManipulator(new ContextualMenuManipulator((act) => { 
                act.menu.AppendAction("Delete", (a) => { OnDeleteSymbol?.Invoke(this, m_Index); }); 
            }));

            this.RegisterCallback<KeyDownEvent>(OnKeyPressed);

            this.style.borderBottomColor = new StyleColor(Color.black);
            this.style.borderBottomWidth = new StyleFloat(0.1f);

            m_Root = new VisualElement();
            m_Root.style.flexDirection = FlexDirection.Row;

            string symbolText = "<i>" + Symbol.TypeToString(m_Symbol.Type) + "</i>:";

            m_SymbolNameTextField = new TextField(name);
            m_SymbolNameTextField.label = symbolText;
            m_SymbolNameTextField.style.maxWidth = 235f;
            m_SymbolNameTextField.style.minWidth = 235f;
            m_SymbolNameTextField.StretchToParentWidth();
            m_SymbolNameTextField.value = m_Symbol.Name;

            m_SymbolNameTextField.RegisterCallback<FocusOutEvent>((evt) =>
            {
                string oldName = m_Symbol.Name;
                string newName = m_SymbolNameTextField.value;

                if (newName == oldName) return;

                if (OnItemNameChange != null)
                {
                    newName = OnItemNameChange(m_Index, oldName, newName);
                }
                m_SymbolNameTextField.value = newName;
                m_Symbol.Name = newName;

                GGNodeEditor.ChangeSymbol(new Symbol(oldName, m_Symbol.Type), new Symbol(m_Symbol.Name, m_Symbol.Type), m_SymbolIndex);
            });

            m_Root.Add(m_SymbolNameTextField);
            Add(m_Root);
        }

        public void SetSymbolType(GraphSymbolType symbolType)
        {
            m_Symbol.Type = symbolType;
            string symbolText = "<i>" + Symbol.TypeToString(m_Symbol.Type) + "</i>:";
            m_SymbolNameTextField.label = symbolText;
        }

        public void SetSymbolName(string symbolName)
        {
            m_Symbol.Name = symbolName;
            m_SymbolNameTextField.value = symbolName;
        }

        public void SetIndex(int absIndex, int symbIndex)
        {
            m_Index = absIndex;
            m_SymbolIndex = symbIndex;
        }


        private void OnKeyPressed(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode == KeyCode.Delete)
            {
                OnDeleteSymbol?.Invoke(this, m_Index);
            }
        }

    }
}