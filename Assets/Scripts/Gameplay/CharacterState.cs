namespace TreasureHunters.Gameplay
{

    /// <summary>
    /// Describes the state of <see cref="CharacterBody"/>.
    /// </summary>
    public enum CharacterState
    {
        /// <summary>
        /// The character stays steady on the ground and can move freely along it.
        /// </summary>
        Grounded,

        /// <summary>
        /// The character is in a state of free fall.
        /// </summary>
        Airborne
    }
}