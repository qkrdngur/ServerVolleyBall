using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DataVisualElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<DataVisualElement, UxmlTraits> { }

    public new class UxmlTraits: VisualElement.UxmlTraits
    {

        private UxmlIntAttributeDescription m_panelIndex = new UxmlIntAttributeDescription
        {
            name="panel-index",
            defaultValue = 0
        };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            var dve = ve as DataVisualElement;

            dve.panelIndex = m_panelIndex.GetValueFromBag(bag, cc);
        }
    }

    public int panelIndex { get; set; }
}
