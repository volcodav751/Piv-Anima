namespace Game.Combat
{
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        /// <summary>Direction of the hit (used for knockback). Can be zero.</summary>
        public readonly Vector2 Direction;
        /// <summary>Who dealt the damage. Can be null.</summary>
        public readonly GameObject Source;

        public DamageInfo(int amount, Vector2 direction = default, GameObject source = null)
        {
            Amount = amount;
            Direction = direction;
            Source = source;
        }
    }
}