using DSharpPlus;

using DSharpPlus.SlashCommands;

using Radarcord;

using Supabase;

using EconoFeast.Commands;

namespace EconoFeast
{
    public class Program
    {
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

            var radar = new RadarcordClient(sender, Globals.Configuration.RadarcordKey);
            
            await radar.PostStatsAsync();

            while (true)
            {
                await UpdateActivityAsync(sender);

                await Task.Delay(15 * 1000);
            }
        }

        private async Task UpdateActivityAsync(DiscordClient client)
        {
            var random = new Random();
            var activity = Globals.Activities[random.Next(Globals.Activities.Count)];

            await client.UpdateStatusAsync(activity);
        }
    }
}
