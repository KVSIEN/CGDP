namespace CGD.Quests
{
    public enum QuestState
    {
        // Prerequisites not yet completed.
        Locked,
        // Can be started.
        Available,
        Active,
        Completed,
        Failed
    }
}
