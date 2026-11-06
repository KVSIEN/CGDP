using System;
using CGD.BehaviorTrees;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace CGD.Editor
{
    // One node on the behavior tree canvas: its parent port on top, its child port below,
    // the title coloured by category, and a summary of its settings. While playing, the
    // border shows what the selected agent's copy of the node last reported.
    internal sealed class BtNodeView : Node
    {
        private const float DebugBorder = 3f;

        private readonly Label _summary;
        private readonly Label _comment;
        private readonly Label _order;

        public BtNodeView(BtNode node)
        {
            Data          = node;
            viewDataKey   = node.Guid;
            title         = BtNodeTypes.DisplayName(node.GetType());
            tooltip       = BtNodeTypes.Description(node.GetType());
            style.minWidth = 160f;

            var category = BtNodeTypes.Category(node);
            titleContainer.style.backgroundColor = BtNodeTypes.Color(category);
            titleButtonContainer.style.display   = DisplayStyle.None;
            if (category == BtNodeCategory.Root) capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);

            _order = new Label { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6f, color = new Color(1f, 1f, 1f, 0.7f) } };
            titleContainer.Insert(0, _order);

            CreatePorts(node);

            _summary = new Label { style = { paddingLeft = 8f, paddingRight = 8f, paddingTop = 4f, whiteSpace = WhiteSpace.Normal } };
            _comment = new Label { style = { paddingLeft = 8f, paddingRight = 8f, paddingBottom = 4f, whiteSpace = WhiteSpace.Normal,
                                              unityFontStyleAndWeight = FontStyle.Italic, color = new Color(0.75f, 0.75f, 0.75f) } };
            extensionContainer.Add(_summary);
            extensionContainer.Add(_comment);
            extensionContainer.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.95f);

            if (node is Subtree)
                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount == 2 && ((Subtree)Data).Tree != null) OpenRequested?.Invoke(((Subtree)Data).Tree);
                });

            SetPosition(new Rect(node.Position, Vector2.zero));
            Refresh();
            RefreshExpandedState();
            RefreshPorts();
        }

        public BtNode Data   { get; }
        public Port   Input  { get; private set; }
        public Port   Output { get; private set; }

        public event Action<BtNodeView> Selected;
        // Double-clicked a Subtree node: open the tree it runs.
        public event Action<BehaviorTreeAsset> OpenRequested;

        // Re-reads the summary and comment after the node's settings change.
        public void Refresh()
        {
            SetLabel(_summary, Data.Summary);
            SetLabel(_comment, Data.Comment);
        }

        // Child number under its parent ("1", "2"…), so execution order is readable at a glance.
        public void SetOrder(int index) => _order.text = index >= 0 ? (index + 1).ToString() : string.Empty;

        public void ShowStatus(BtStatus status, bool live)
        {
            Color color = BtNodeTypes.StatusColor(status);
            color.a = live ? 1f : 0.35f;
            SetBorder(color, DebugBorder);
        }

        public void ClearStatus() => SetBorder(Color.clear, 0f);

        public override void OnSelected()
        {
            base.OnSelected();
            Selected?.Invoke(this);
        }

        // Ports are moved out of the side-by-side "top" row: parent link above, children below.
        private void CreatePorts(BtNode node)
        {
            if (node is not BtRoot)
            {
                Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                Input.portName = string.Empty;
                inputContainer.Add(Input);
            }

            if (node is BtParentNode parent)
            {
                var capacity = parent.MaxChildren == 1 ? Port.Capacity.Single : Port.Capacity.Multi;
                Output = InstantiatePort(Orientation.Vertical, Direction.Output, capacity, typeof(bool));
                Output.portName = string.Empty;
                outputContainer.Add(Output);
            }

            inputContainer.style.alignItems  = Align.Center;
            outputContainer.style.alignItems = Align.Center;
            mainContainer.Insert(0, inputContainer);
            mainContainer.Add(outputContainer);
            topContainer.style.display = DisplayStyle.None;
        }

        private void SetBorder(Color color, float width)
        {
            var s = mainContainer.style;
            s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = color;
            s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = width;
        }

        private static void SetLabel(Label label, string text)
        {
            label.text          = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
