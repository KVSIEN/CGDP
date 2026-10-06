namespace CGD.Input
{
    public enum GameAction
    {
        Jump,
        Sprint,
        Crouch,
        Dodge,
        Attack,
        Melee,
        AimDownSights,
        Reload,
        Interact,
        Ability1,
        Ability2,
        Ability3,
        Ability4,
        Pause,
        TogglePerspective,
        Inventory,
        Map,
        ShoulderSwap,
        Weapon1,
        Weapon2,
        Weapon3,
        Weapon4,
        LockOn,
        Console,
        Character,
        Item1,
        Item2,
        Item3,
        Item4,
        WeaponMode,     // bows: switch between a vertical and a horizontal draw
        NextWeapon,     // cycles to the next filled weapon slot (scroll down)
        PreviousWeapon, // cycles to the previous filled weapon slot (scroll up)
        LastWeapon,     // swaps back to the weapon used before the current one (unbound by default)
    }
}
