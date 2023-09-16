using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace EconoFeast.Commands
{
    public enum ColorChoice
    {
        [ChoiceName("Skin")]
        SkinColor,
        [ChoiceName("Stomach")]
        StomachColor,
        [ChoiceName("Acid")]
        AcidColor
    }

    [SlashCommandGroup("config", "Configuration Commands")]
    public class ConfigCommands : ApplicationCommandModule
    {
        public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
        {
            return await Utils.PreFlightChecksWithOptOut(ctx);
        }

        [SlashCommand("logchannel", "Get or set your log channel ID")]
        [SlashCommandPermissions(Permissions.ManageGuild)]
        public async Task LogChannelConfigCommand(InteractionContext ctx, [Option("channel", "The channel to set as the log channel, defaults to null (get current channel)")] DiscordChannel? channel = null)
        {
            await ctx.DeferAsync();
            var guild = await Utils.GetGuildAsync(ctx.Guild);

            if (channel is null)
            {
                DiscordChannel? discordChannel;

                try
                {
                    discordChannel = await ctx.Client.GetChannelAsync(Convert.ToUInt64(guild.LogChannelId));
                }
                catch (DSharpPlus.Exceptions.NotFoundException)
                {
                    discordChannel = null;
                }

                var embed = Utils.MakeEmbed(DiscordColor.Cyan, description: $"Your current log channel is {Utils.EvaluateBool(discordChannel is not null, $"<#{discordChannel!.Id}>", "None/Not Found")}");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            if (channel.Type != ChannelType.Text)
            {
                var errorEmbed = Utils.MakeErrorEmbed("Channel provided is not a text channel!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var botMember = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            var permissions = channel.PermissionOverwrites;

            foreach (var permission in permissions)
            {
                if (botMember.Permissions.HasPermission(Permissions.Administrator)) break;
                var affectedMember = await permission.GetMemberAsync();

                if (affectedMember.Id != botMember.Id) continue;

                if (!permission.Allowed.HasPermission(Permissions.SendMessages))
                {
                    var errorEmbed = Utils.MakeErrorEmbed($"I don't have the `Send Messages` permission in <#{channel.Id}>, please make sure to add a permission overwrite for me (as I'm still learning to comprehend roles) in the specified channel!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                    return;
                }
            }

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Success", $"Log channel set to <#{channel.Id}>!");
            await Utils.UpdateGuildAsync(ctx.Guild, x => x.LogChannelId, channel.Id.ToString());
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }

        [SlashCommand("color", "Get or set one of your cosmetic colors")]
        public async Task ColorConfigCommand(InteractionContext ctx, [Option("type", "The type of color to get or set")] ColorChoice colorType, [Option("value", "The new value")] string? value = null)
        {
            await ctx.DeferAsync();

            string colorTypeName = colorType switch
            {
                ColorChoice.AcidColor => "Acid Color",
                ColorChoice.SkinColor => "Skin Color",
                ColorChoice.StomachColor => "Stomach Color",
                _ => throw new ArgumentException("Invalid color type provided")
            };

            var user = await Utils.GetUserAsync(ctx.User);

            if (value is null)
            {
                string hex = colorType switch
                {
                    ColorChoice.AcidColor => user.AcidColor,
                    ColorChoice.SkinColor => user.SkinColor,
                    ColorChoice.StomachColor => user.StomachColor,
                    _ => throw new ArgumentException("Invalid color type provided")
                };

                var currentColorEmbed = Utils.MakeEmbed(new DiscordColor(hex), $"Current {colorTypeName}", $"View the color of this embed to see a preview of your color (`{hex}`).");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(currentColorEmbed));
                return;
            }

            string hexRegex = @"#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})\b";

            var valueIsHex = Regex.IsMatch(value, hexRegex);

            if (!valueIsHex)
            {
                var errorEmbed = Utils.MakeErrorEmbed("That's not a valid hex code! A valid hex code would be this, for example: `#00ff02`");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            Expression<Func<User, object>> keySelector = colorType switch
            {
                ColorChoice.SkinColor => x => x.SkinColor,
                ColorChoice.StomachColor => x => x.StomachColor,
                ColorChoice.AcidColor => x => x.AcidColor,
                _ => throw new ArgumentException("Invalid color type provided")
            };

            await Utils.UpdateUserAsync(ctx.User, keySelector, value);

            var embed = Utils.MakeEmbed(new DiscordColor(value), "Color set!", $"Your **{colorTypeName}** was changed to **`{value}`**, view the color of this embed to see this color.");

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("dm_status", "Get or set whether or not you want to receive DMs from the bot")]
        public async Task DmStatusConfigCommand(InteractionContext ctx, [Option("status", "The new status")] bool? status = null)
        {
            await ctx.DeferAsync();

            var user = await Utils.GetUserAsync(ctx.User);

            if (status is null)
            {
                var embed = Utils.MakeEmbed(DiscordColor.Cyan, description: $"Your current DM status is **{Utils.EvaluateBool(user.CanBeDmed, "Enabled", "Disabled")}**");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            await Utils.UpdateUserAsync(ctx.User, x => x.CanBeDmed, status.Value);

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Success", $"Your DM status was set to **{Utils.EvaluateBool(status.Value, "Enabled", "Disabled")}**!");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }

        [SlashCommand("language_filter", "Get or set whether or not you want to have swear words filtered in this guild")]
        [SlashCommandPermissions(Permissions.ManageGuild)]
        public async Task LanguageFilterConfigCommand(InteractionContext ctx, [Option("status", "The new status")] bool? status = null)
        {
            await ctx.DeferAsync();

            var guild = await Utils.GetGuildAsync(ctx.Guild);

            if (status is null)
            {
                var embed = Utils.MakeEmbed(DiscordColor.Cyan, description: $"Current guild has language filter set to **{Utils.EvaluateBool(guild.Options.UsesLanguageFilter, "Enabled", "Disabled")}**");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            var options = new GuildOptions
            {
                UsesLanguageFilter = status.Value
            };
            await Utils.UpdateGuildAsync(ctx.Guild, x => x.Options, options);

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Success", $"The language filter status of this guild was set to **{Utils.EvaluateBool(status.Value, "Enabled", "Disabled")}**!");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }
    }
}
