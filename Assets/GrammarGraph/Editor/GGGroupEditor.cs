using GG.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GG.Editor
{
    public class GGGroupEditor : Group
    {
        public string ID { get; set; }

        private float _weight = 1.0f;
        public float Weight { get { return _weight; } set { if (value >= 0) _weight = value; m_WeightTextField.value = value.ToString(); } }

        private TextField m_WeightTextField;

        public Vector2 Position = Vector2.zero;

        public GGGroupEditor()
        {
            m_WeightTextField = new TextField() { label = "Weight", value = "1"};
            m_WeightTextField.ElementAt(0).style.minWidth = 10f;
            m_WeightTextField.maxLength = 5;
            m_WeightTextField.style.maxWidth = 100f;
            
            m_WeightTextField.RegisterCallback<FocusOutEvent>(e => 
            {
                float newVal;
                if (float.TryParse(m_WeightTextField.value, out newVal) && newVal >= 0)
                {
                    Weight = newVal;
                }
                
                m_WeightTextField.value = Weight.ToString();

            });

            this.Insert(0,m_WeightTextField);

        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            foreach (GraphElement element in elements)
            {
                if (element is GGNodeEditor node)
                {
                    node.Group = this;
                }

            }
            base.OnElementsAdded(elements);
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            foreach (GraphElement element in elements)
            {
                if (element is GGNodeEditor node)
                {
                    node.Group = null;
                }


            }

            base.OnElementsRemoved(elements);
        }
    }
}
