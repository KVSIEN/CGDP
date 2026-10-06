namespace CGD.Level
{
    // What one step of a map objective asks for, in the room it's assigned to.
    public enum ObjectiveAction
    {
        Clear,    // kill every enemy in the room
        Activate, // switch on a console placed in the room
        Retrieve, // pick up an item placed in the room
    }
}
