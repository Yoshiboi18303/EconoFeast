using DSharpPlus;
using DSharpPlus.SlashCommands;

using Supabase;

using EconoFeast.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace EconoFeast
{
    public class Program
    {
        private bool HasSentHelloEmbed { get; set; } = false;
        private bool ReconnectionAttempted { get; set; } = false;

        public static async Task Main() => await new Program().StartBot();

        private async Task StartBot()
        {
            try
            {
                Globals.Configuration = await Utils.GetConfigurationAsync();
            }
            catch (Exception)
            {
                Logger.Error("Couldn't get configuration so we can't start the bot! Exiting...");
                Environment.Exit(1);
                return;
            }

            Globals.Items = Utils.SetupItems();

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = Globals.Configuration.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
            });
            Globals.SupabaseClient = new(Globals.Configuration.SupabaseUrl, Globals.Configuration.SupabaseKey, new SupabaseOptions()
            {
                AutoConnectRealtime = true,
            });

            List<Type> moduleTypes = new()
            {
                typeof(GuildOnlyCommands),
                typeof(InfoCommands),
                typeof(EconomyCommands),
                typeof(OwnerOnlyCommands),
                typeof(OtherCommands),
                typeof(ConfigCommands)
            };

            discord.GuildDeleted += OnLeftGuild;
            discord.GuildCreated += OnJoinedGuild;
            discord.Ready += OnClientReady;
            discord.ClientErrored += OnClientEventError;
            discord.Zombied += OnClientZombied;

            var slash = discord.UseSlashCommands();

            await Utils.SetupCommands(slash, moduleTypes, 1018935272244789329);

            await Globals.SupabaseClient.InitializeAsync();
            await discord.ConnectAsync();

            await Task.Delay(-1);
        }

        private async Task OnClientZombied(DiscordClient sender, ZombiedEventArgs args)
        {
            Logger.Error("We were zombied due to too many failed heartbeats! Attempting to reconnect, if an attempt was not made already...");

            if (ReconnectionAttempted)
            {
                Logger.Error("Reconnection attempt already made, exiting...");
                Environment.Exit(1);
            }
            else
            {
                await sender.ReconnectAsync(true);
                ReconnectionAttempted = true;
            }
        }

        private Task OnClientEventError(DiscordClient sender, ClientErrorEventArgs args)
        {
            Logger.Error($"An exception occurred in event: {args.EventName}\n\nException: {args.Exception.Message}");

            #if DEBUG

                Logger.Error($"Stack Trace: {args.Exception.StackTrace}");

            #endif

            return Task.CompletedTask;
        }

        private async Task OnLeftGuild(DiscordClient sender, GuildDeleteEventArgs args)
        {
            var logChannel = await sender.GetChannelAsync(Globals.BotLogChannelId);

            var fields = new List<FieldData>()
            {
                new FieldData("Guild Name", args.Guild.Name, true)
            };
            var embed = Utils.MakeEmbed(DiscordColor.Yellow, "Removed from guild...", "I was sadly removed from a guild...", fields: fields);

            await logChannel.SendMessageAsync(embed);
        }

        private async Task OnJoinedGuild(DiscordClient sender, GuildCreateEventArgs args)
        {
            HasSentHelloEmbed = false;

            var guild = args.Guild;
            var channels = await guild.GetChannelsAsync();
            var hellChannels = channels.Where(x =>
                x.Name == "bot-hell" ||
                x.Name.Contains("hell"));

            // We get to validation of hellChannels later, first we send something to the log channel.
            var logChannel = await sender.GetChannelAsync(Globals.BotLogChannelId);
            var fields = new List<FieldData>()
            {
                new FieldData("Guild Name", guild.Name)
            };
            var addedEmbed = Utils.MakeEmbed(DiscordColor.Green, "Added to guild", "I was added to a new guild!", fields: fields);

            await logChannel.SendMessageAsync(addedEmbed);

            if (!hellChannels.Any()) return;

            var welcomeFileContents = await File.ReadAllTextAsync(@"C:\Users\yoshi\OneDrive\Desktop\EconoFeast\EconoFeast\BOT-ADD-MESSAGE.md");
            var welcomeContents = welcomeFileContents
                .Replace("[bot_name]", sender.CurrentUser.Username)
                .Replace("[guild_name]", guild.Name);

            var helloEmbed = Utils.MakeEmbed(DiscordColor.Cyan, "Hello!", welcomeContents);

            await SendMessage(hellChannels, helloEmbed, guild);

            if (!HasSentHelloEmbed) Logger.Error("Failed to send hello embed in any of the specified hellChannels!");
        }

        private async Task SendMessage(IEnumerable<DiscordChannel> hellChannels, DiscordEmbed embed, DiscordGuild guild)
        {
            foreach (var hellChannel in hellChannels)
            {
                if (HasSentHelloEmbed) break;

                await AttemptSendEmbed(hellChannel, embed, guild);
            }
        }

        private async Task AttemptSendEmbed(DiscordChannel hellChannel, DiscordEmbed embed, DiscordGuild guild)
        {
            try
            {
                await hellChannel.SendMessageAsync(embed);
                HasSentHelloEmbed = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send hello embed to {hellChannel.Name} in {guild.Name}");
                Logger.Error($"Exception: {ex.Message}");
                Logger.Info("Moving to next channel...");
            }
        }

        private async Task OnClientReady(DiscordClient sender, ReadyEventArgs args)
        {
            Globals.StartDate = DateTime.Now;

            Logger.Success("Connected to Discord!");

            var radar = Globals.GetRadarcordClient(sender);

            await radar.PostStatsAsync();

            while (true)
            {
                await UpdateActivityAsync(sender);

                await Task.Delay(15 * 1000);
            }
        }

        private static async Task UpdateActivityAsync(DiscordClient client)
        {
            var random = new Random();
            var activity = Globals.Activities[random.Next(Globals.Activities.Count)];

            await client.UpdateStatusAsync(activity);
        }
    }
}
