namespace EconoFeast.Attributes;

/// <summary>
/// Marks a CommandModule as a guild only module.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GuildOnlyAttribute : Attribute
{
    /// <summary>
    /// Marks a CommandModule as a guild only module.
    /// </summary>
    public GuildOnlyAttribute() { }
}
