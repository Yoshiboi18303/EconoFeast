using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace ThingBot.Commands
{
    [Attributes.GuildOnly]
    [SlashCommandGroup("testing", "Some commands that have to be tested.")]
    public class GuildOnlyCommands : ApplicationCommandModule
    {
        
    }
}
