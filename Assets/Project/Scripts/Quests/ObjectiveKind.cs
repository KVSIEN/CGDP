namespace CGD.Quests
{
    // What an objective counts. The objective's Target says which one:
    //   Kill    → an EnemyData (any enemy using that data)
    //   Collect → an ItemDefinition picked up into the inventory
    //   Signal  → a QuestSignal raised by the scene (interactions, zones, scripted beats)
    public enum ObjectiveKind
    {
        Kill,
        Collect,
        Signal
    }
}
