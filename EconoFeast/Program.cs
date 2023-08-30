using DSharpPlus;

using DSharpPlus.SlashCommands;

using Supabase;

using ThingBot.Commands;

namespace ThingBot
{
    public class Program
    {
        public static async Task Main() => await new Program().StartBot();

        private async Task StartBot()
        {
            Configuration config;

            try
            {
                config = await Utils.GetConfigurationAsync();
            }
            catch (Exception)
            {
                Logger.Error("Couldn't get config so we can't start the bot! Exiting...");
                Environment.Exit(1);
                return;
            }

            Globals.Items = Utils.SetupItems();

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = config.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged,
            });
            Globals.SupabaseClient = new(config.SupabaseUrl, config.SupabaseKey, new SupabaseOptions()
            {
                AutoConnectRealtime = true,
            });

            List<Type> moduleTypes = new()
            {
                typeof(GuildOnlyCommands),
                typeof(InfoCommands),
                typeof(EconomyCommands),
                typeof(OwnerOnlyCommands)
            };

            discord.Ready += OnClientReady;

            var slash = discord.UseSlashCommands();

            await Utils.SetupCommands(slash, moduleTypes, 1018935272244789329);

            await Globals.SupabaseClient.InitializeAsync();
            await discord.ConnectAsync();

            await Task.Delay(-1);
        }

        private async Task OnClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Globals.StartDate = DateTime.Now;

            Logger.Success("Connected to Discord!");

            await ActivityLoopAsync(sender);
        }

        private async Task ActivityLoopAsync(DiscordClient client)
        {
            var random = new Random();
            var activity = Globals.Activities[random.Next(Globals.Activities.Count)];

            await client.UpdateStatusAsync(activity);

            await Task.Delay(15 * 1000);

            await ActivityLoopAsync(client);
        }
    }
}
