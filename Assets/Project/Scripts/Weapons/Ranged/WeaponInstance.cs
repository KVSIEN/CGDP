namespace CGD.Weapons
{
    // Runtime state of one carried weapon, so its ammo survives swaps and drops.
    public class WeaponInstance
    {
        public WeaponData Data { get; }
        public int Magazine { get; internal set; }
        public int Reserve  { get; internal set; }

        public WeaponInstance(WeaponData data)
        {
            Data = data;
            Refill();
        }

        public void Refill()
        {
            Magazine = Data.MagazineSize;
            Reserve  = Data.GetNormalizedReserveAmmo();
        }
    }
}
