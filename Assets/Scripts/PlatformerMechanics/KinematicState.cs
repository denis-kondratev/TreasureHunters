namespace TreasureHunters.PlatformerMechanics
{
    /// <summary>
    /// Describes the states in which a <see cref="KinematicObject"/> can be.
    /// </summary>
    public enum KinematicState
    {
        /// <summary>
        /// The object is on a surface and can move along this surface to the
        /// right or left.
        /// </summary>
        Grounded,
        
        /// <summary>
        /// The object is in the air and can move in any direction. In this
        /// case, it can be said that the object is in a form of controlled
        /// free fall, although with some qualifications. For example, if it
        /// is a character controlled by the player, they can still influence
        /// its movement to the left or right to some extent.
        /// </summary>
        Airborne
    }
}