using Redbox.HAL.Component.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Redbox.HAL.Common.GUI.Functions
{
    public sealed class ConfigItem
    {
        private object m_value;

        public ConfigItem(XmlNode node)
        {
            this.Node = node;
            this.CustomEditor = node.GetAttributeValue<string>("custom-editor");
            this.ItemType = Type.GetType(node.GetAttributeValue<string>("type", typeof(string).ToString()));
            this.ReadOnly = node.GetAttributeValue<bool>("read-only", false);
            this.Name = node.GetAttributeValue<string>("name");
            this.m_value = this.CustomEditor != null ? (object)this.CustomEditor : (object)node.GetAttributeValue<string>("value");
            this.DefaultValue = this.m_value;
            this.Description = node.GetAttributeValue<string>("description");
            this.DisplayName = node.GetAttributeValue<string>("display-name");
            this.CategoryName = node.GetAttributeValue<string>("category");
            this.ValidValuesCount = node.GetAttributeValue<int>("valid-value-count", 0);
            if (this.ValidValuesCount <= 0)
                return;
            this.ValidValues = new List<string>();
            foreach (XmlNode childNode in node.ChildNodes)
            {
                string attributeValue = childNode.GetAttributeValue<string>("value");
                if (attributeValue != null)
                    this.ValidValues.Add(attributeValue);
            }
            if (this.Value != null && !this.ValidValues.Contains(this.Value.ToString()))
                this.ValidValues.Add(this.Value.ToString());
            this.ItemType = this.ValidValues.GetType();
        }

        public bool ReadOnly { get; private set; }

        public string Name { get; private set; }

        public object Value
        {
            get => this.m_value;
            set
            {
                this.m_value = value;
                this.Node.SetAttributeValue<string>(nameof(value), value.ToString());
            }
        }

        public string Description { get; private set; }

        public object DefaultValue { get; set; }

        public string DisplayName { get; set; }

        public string CategoryName { get; private set; }

        public List<string> ValidValues { get; private set; }

        public int ValidValuesCount { get; private set; }

        public string CustomEditor { get; private set; }

        public Type ItemType { get; private set; }

        public XmlNode Node { get; private set; }
    }
}
