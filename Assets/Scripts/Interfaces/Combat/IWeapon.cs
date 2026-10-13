namespace Game.Combat
{
    public interface IWeapon
    {
        bool IsReady { get; }

        bool TryFire(); //стріляє і повертає true якщо вистрелило, а під час перезарядки чи любої затримки повертає False
        void StopFiring();
    }
}