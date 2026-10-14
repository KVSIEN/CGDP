namespace CGD.Weapons
{
    // How a melee model is held: as its type is (WeaponCategory.OneHanded), or always in one
    // or both hands — a Zweihänder leaves no hand free for a shield whatever sword type it is.
    public enum MeleeGrip { AsType, OneHanded, TwoHanded }
}
