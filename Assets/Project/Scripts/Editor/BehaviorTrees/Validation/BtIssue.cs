namespace CGD.Editor
{
    // One problem the validator found, on a node (or the tree as a whole when NodeGuid is null).
    internal readonly struct BtIssue
    {
        public readonly string          NodeGuid;
        public readonly BtIssueSeverity Severity;
        public readonly string          Message;

        public BtIssue(string nodeGuid, BtIssueSeverity severity, string message)
        {
            NodeGuid = nodeGuid;
            Severity = severity;
            Message  = message;
        }

        public string Icon => Severity switch
        {
            BtIssueSeverity.Error   => "⛔",
            BtIssueSeverity.Warning => "⚠",
            _                       => "ⓘ",
        };
    }
}
