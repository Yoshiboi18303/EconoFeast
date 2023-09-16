using DSharpPlus;
using DSharpPlus.AsyncEvents;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

using System.Linq.Expressions;

namespace EconoFeast.Commands
{
    [SlashCommandGroup("economy", "The main part of the bot!")]
    public class EconomyCommands : ApplicationCommandModule
    {
        public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
        {
            return await Utils.PreFlightChecksWithOptOut(ctx);
        }

        [SlashCommand("eat", "Turn a user into your meal, this can only be done 1 time every 3 minutes.")]
        [SlashCooldown(1, 180, SlashCooldownBucketType.User)]
        public async Task EatUserCommand(InteractionContext ctx, [Option("user", "The user to target")] DiscordUser userToEat)
        {
            await ctx.DeferAsync();

            var currentGuild = await Utils.GetGuildAsync(ctx.Guild);

            #region Checks

            if (Globals.Configuration.OptedOutUserIds.Contains(userToEat.Id))
            {
                var errorEmbed = Utils.MakeErrorEmbed($"{userToEat.Username} has opted out of the bot, you cannot eat them!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (userToEat.IsBot)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"You can't just eat my kind, what in the holy {Utils.LanguageFilter(ctx.Guild, "heck", "hell")} have they done to you?");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (userToEat.Id == ctx.User.Id)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"You can't just eat yourself, what the {Utils.LanguageFilter(ctx.Guild, "flip", "fuck")} do you think this is?!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var currentUser = await Utils.GetUserAsync(ctx.User);
            var targetUser = await Utils.GetUserAsync(userToEat);

            if (currentUser.LastMealTime.HasValue)
            {
                Logger.Info("User has a last meal time.");

                var timeBetween = (DateTime.Now - currentUser.LastMealTime).Value;

                if (timeBetween.TotalMinutes < 3)
                {
                    var errorEmbed = Utils.MakeErrorEmbed("You should calm it, there needs to be 3 minutes between your last person. Sorry, I don't make the rules.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                    return;
                }
            }

            if (currentUser.IsInStomach)
            {
                var captor = Utils.GetCaptorAsync(currentUser);
                var captorDiscord = await ctx.Client.GetUserAsync(Convert.ToUInt64(captor.Id));
                var errorEmbed = Utils.MakeErrorEmbed($"You're inside of {captorDiscord.Username}, you can't do anything while you're inside of them!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (Utils.UserIsFull(currentUser))
            {
                var errorEmbed = Utils.MakeErrorEmbed("You're completely stuffed, one more person and you'll pop like a balloon *(and we can't have that)*!\n\n**Tip:** Use the `digest` command for a chance to increase your stomach capacity!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (targetUser.IsInStomach)
            {
                var captor = await Utils.GetCaptorAsync(targetUser);
                var captorDiscord = await ctx.Client.GetUserAsync(Convert.ToUInt64(captor!.Id));
                var errorEmbed = Utils.MakeErrorEmbed($"{userToEat.Username} is inside of {captorDiscord.Username}, you cannot eat them while someone else has them!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            if (!targetUser.CanBeEaten)
            {
                var errorEmbed = Utils.MakeErrorEmbed($"{userToEat.Username} has opted out of being eaten!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            // Final check, ensure the targeted user doesn't have one of the Rat Poison item.
            // If they do, check whether to give all the bones within the end user to the targetted user.

            if (targetUser.Items.RatPoison > 0)
            {
                if (currentUser.AmountOfBonesInStomach == 0)
                {
                    var embed1 = Utils.MakeEmbed(DiscordColor.Black, description: $"Looks like {userToEat.Username} had some rat poison, but you have zero bones inside of you, so no one gets anything.\n\n**To `{userToEat.Username}`**, you have not lost your item due to fairness.", footer: new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "I may be here to watch chaos break out, but I have to be fair."
                    });
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed1));
                    return;
                }

                var newItemAmounts = new ItemAmounts()
                {
                    AcidSpit = targetUser.Items.AcidSpit,
                    DigestionMedicine = targetUser.Items.DigestionMedicine,
                    Lube = targetUser.Items.Lube,
                    RatPoison = targetUser.Items.RatPoison - 1,
                    SecretRevealer = targetUser.Items.SecretRevealer
                };
                await Utils.UpdateUserAsync(userToEat, x => x.Items, newItemAmounts);
                var random1 = new Random();

                if (random1.NextDouble() >= 0.69 /* Nice. */)
                {
                    // End user keeps their bones.
                    var embed2 = Utils.MakeEmbed(DiscordColor.Yellow, description: $"{userToEat.Username} had some rat poison, however you got to keep your bones!");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed2));
                    return;
                }
                else
                {
                    // Targetted user gets the bones the end user had.
                    var embed3 = Utils.MakeEmbed(DiscordColor.Red, description: $"{userToEat.Username} had some rat poison, and looks like they're making away with all the bones that were in you!", footer: new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Might I suggest extracting? Might save you next time!"
                    });

                    await Utils.UpdateUserAsync(ctx.User, x => x.AmountOfBonesInStomach, Convert.ToUInt64(0));
                    await Utils.UpdateUserAsync(userToEat, x => x.AmountOfBonesCollected, targetUser.AmountOfBonesCollected + currentUser.AmountOfBonesInStomach);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed3));
                    return;
                }
            }

            #endregion

            var random = new Random();

            var peopleInStomachList = currentUser.PeopleInStomach;
            peopleInStomachList.Add(targetUser.Id);

            // Set up value update lists.
            var currentUserValues = new List<object>()
            {
                peopleInStomachList,
                currentUser.AmountOfPeopleInStomach + 1,
                DateTime.Now
            };

            var targetUserValues = new List<object>()
            {
                true,
                random.NextSingle(),
            };

            // Calculate success based on how many people are in the user.
            // If targetUser.AmountOfPeopleInStomach is 0, automatic success.
            bool success = true;
            var amountOfPeopleInTargetUser = targetUser.AmountOfPeopleInStomach;

            const double BASE_CALCULATION_NUMBER = 0.5;
            const double CALCULATION_INCREMENT = 0.05;
            const double MAX_CALCULATION_NUMBER = 0.75;

            if (amountOfPeopleInTargetUser > 0)
            {
                if (amountOfPeopleInTargetUser >= 5) success = random.NextDouble() > MAX_CALCULATION_NUMBER;
                else
                {
                    var max = BASE_CALCULATION_NUMBER;
                    while (amountOfPeopleInTargetUser > 0)
                    {
                        max += CALCULATION_INCREMENT;
                        amountOfPeopleInTargetUser -= 1;
                    }
                    success = random.NextDouble() > max;
                }
            }

            if (!success)
            {
                var errorEmbed = Utils.MakeEmbed(DiscordColor.Red, "Failed", $"You have failed to swallow {userToEat.Username}, maybe next time.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            // Update users
            await Utils.UpdateUserAsync(ctx.User, new List<Expression<Func<User, object>>>()
            {
                x => x.PeopleInStomach,
                x => x.AmountOfPeopleInStomach,
                x => x.LastMealTime
            }, currentUserValues);

            await Utils.UpdateUserAsync(userToEat, new List<Expression<Func<User, object>>>()
            {
                x => x.IsInStomach,
                x => x.Softness,
            }, targetUserValues);

            var embed = Utils.MakeEmbed(DiscordColor.Green, description: $"You have successfully swallowed <@{userToEat.Id}>, let's hope they get comfy inside of you!");
            var eatenEmbed = Utils.MakeEmbed(DiscordColor.Yellow, description: $"You were swallowed by **{Utils.MakeFirstCharacterUppercase(ctx.User.Username)}**! Let's hope they're nice to you...");
            var member = await ctx.Guild.GetMemberAsync(userToEat.Id);

            if (member is not null && targetUser.CanBeDmed)
            {
                try
                {
                    await member.SendMessageAsync(eatenEmbed);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Couldn't DM {Utils.MakeFirstCharacterUppercase(userToEat.Username)} the eaten embed!");

                    #if DEBUG

                    Logger.Error($"Exception message: {ex.Message}");

                    #endif
                }
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("digest", "Finish off your prey")]
        [SlashCooldown(1, 300.0, SlashCooldownBucketType.User)]
        public async Task DigestUsersCommand(InteractionContext ctx)
        {
            // Plan: Make this a minigame rather than an immediate reward.
            await ctx.DeferAsync();
            var user = await Utils.GetUserAsync(ctx.User);

            if (user.AmountOfPeopleInStomach == 0)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You don't have anyone inside of you, you can't digest nothing!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var random = new Random();

            // Formula: baseNumber * number of people inside the user
            // Adding in the amount of bones in each person inside (not the ones the user already removed, kinda like a bank), if they have any.
            //
            // Example: 206 * 3 + 412 + 206 + 0 = 824 bones, my math could be way off though.
            // Yes, this was taken from my old code. Sue me.
            var calculated = Globals.BaseBonesNumber * user.AmountOfPeopleInStomach;
            var shouldStomachCapacityIncrease = random.NextDouble() > 0.69;
            var increaseInStomachCapacity = Convert.ToUInt64(Math.Ceiling(Convert.ToDecimal(Utils.GetRandomDouble(5, random))));

            #region People Inside User Updates
            foreach (var personId in user.PeopleInStomach)
            {
                var discordId = Convert.ToUInt64(personId);
                var userInStomach = await Utils.GetUserAsync(personId);
                var bonesLost = userInStomach.AmountOfBonesInStomach;

                calculated += (uint)bonesLost;

                var personInStomachValues = new List<object>()
                {
                    Convert.ToUInt64(0), // amountOfBonesInStomach
                    false, // inStomach
                    0.0f // softness
                };

                await Utils.UpdateUserAsync(personId, new List<Expression<Func<User, object>>>()
                {
                    x => x.AmountOfBonesInStomach,
                    x => x.IsInStomach,
                    x => x.Softness
                }, personInStomachValues);

                var member = await ctx.Guild.GetMemberAsync(discordId);

                if (member is not null && userInStomach.CanBeDmed && userInStomach.AmountOfBonesInStomach > 0)
                {
                    var digestedEmbed = Utils.MakeEmbed(DiscordColor.Red, description: $"**Whoopsies!** You just got digested and lost all {bonesLost} of the bones inside of you!", footer: new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Tip: Remove those bones next time, please..."
                    });

                    try
                    {
                        await member.SendMessageAsync(digestedEmbed);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Couldn't DM {Utils.MakeFirstCharacterUppercase(member.Username)} the digested embed!");

                        #if DEBUG

                        Logger.Error($"Exception message: {ex.Message}");

                        #endif
                    }
                }
            }
            #endregion

            var currentUserValues = new List<object>()
            {
                new List<string>(), // peopleInStomach
                Convert.ToUInt32(0), // amountOfPeopleInStomach
                user.AmountOfBonesInStomach + Convert.ToUInt64(calculated), // bonesInStomach
            };
            var keySelectors = new List<Expression<Func<User, object>>>()
            {
                x => x.PeopleInStomach,
                x => x.AmountOfPeopleInStomach,
                x => x.AmountOfBonesInStomach
            };

            string description = $"You have successfully digested {Utils.EvaluateBool(user.AmountOfPeopleInStomach == 1, $"the {user.AmountOfPeopleInStomach} person", $"all {user.AmountOfPeopleInStomach} people")} inside of you, you have earned **{calculated}** bones.";

            if (shouldStomachCapacityIncrease)
            {
                currentUserValues.Add(increaseInStomachCapacity); // Add stomachCapacity to list
                description += $"\n\n**Also, lucky you! Your stomach decided to hold more people! Your stomach capacity has increased by {increaseInStomachCapacity}!**";
            }

            if (currentUserValues.Count == 4) keySelectors.Add(x => x.StomachCapacity);

            await Utils.UpdateUserAsync(user.Id, keySelectors, currentUserValues);

            var embed = Utils.MakeEmbed(DiscordColor.Green, description: description);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("extract", "Extract all bones from your stomach")]
        public async Task ExtractBonesCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var user = await Utils.GetUserAsync(ctx.User);

            if (user.AmountOfBonesInStomach == 0)
            {
                var errorEmbed = Utils.MakeErrorEmbed("You have no bones inside of you!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            // Formula for this:
            // Amount of bones in the user - Amount of bones lost due to digestion (0 if the user has the Digestion Medicine item).
            // Example: 206 (bones in stomach) - 52 (bones lost) = 152 (total)
            var random = new Random();
            var hasDigestionMedicine = user.Items.DigestionMedicine > 0;
            var bonesLost = Utils.EvaluateBool(hasDigestionMedicine, 0, random.NextInt64(1, Convert.ToInt64(user.AmountOfBonesInStomach / 2)));
            var totalCollected = user.AmountOfBonesInStomach - Convert.ToUInt64(bonesLost);

            var keySelectors = new List<Expression<Func<User, object>>>()
            {
                x => x.AmountOfBonesInStomach,
                x => x.AmountOfBonesCollected
            };
            var values = new List<object>()
            {
                Convert.ToUInt64(0),
                user.AmountOfBonesCollected + totalCollected
            };

            if (hasDigestionMedicine)
            {
                keySelectors.Add(x => x.Items);
                values.Add(new ItemAmounts()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine - 1,
                    Lube = user.Items.Lube,
                    RatPoison = user.Items.RatPoison,
                });
            }

            await Utils.UpdateUserAsync(user.Id, keySelectors, values);

            string description = Utils.EvaluateBool(bonesLost == 0, $"Your digestion medicine has allowed you to extract all {totalCollected} bones from your stomach, good purchase!", $"You have extracted {totalCollected} bones from your stomach, however the other {user.AmountOfBonesInStomach - totalCollected} bones were lost to digestion.");

            var embed = Utils.MakeEmbed(Utils.EvaluateBool(bonesLost == 0, DiscordColor.Green, DiscordColor.Yellow), description: description);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("shop", "View the entire shop, or an item within it.")]
        public async Task ShopCommand(InteractionContext ctx, [Option("id", "The item ID to look for")] string? id = null)
        {
            await ctx.DeferAsync();

            #region Item Details

            if (id is not null)
            {
                if (!Globals.Items.ContainsKey(id))
                {
                    var noItemEmbed = Utils.MakeErrorEmbed($"No item with an ID of `{id}` was found.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noItemEmbed));
                    return;
                }

                if (Globals.Items.TryGetValue(id, out var item))
                {
                    var itemFields = new List<FieldData>()
                    {
                        new FieldData("Price", $"{Utils.ToLocaleString(item.Price)} bones", true),
                        new FieldData("Purchase Command", $"`/buy {item.Id}`", true)
                    };
                    var itemEmbed = Utils.MakeEmbed(DiscordColor.Cyan, item.Name, item.Description, fields: itemFields);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(itemEmbed));
                    return;
                }
            }

            #endregion

            #region Get Initial Page

            var shopItems = Globals.Items.ToArray();

            // Note to self: 5 items per page, remember this.
            var pages = new Dictionary<int, List<ShopItem>>()
            {
                {
                    0,
                    new List<ShopItem>()
                    {
                        shopItems[0].Value,
                        shopItems[1].Value,
                        shopItems[2].Value,
                        shopItems[3].Value,
                        shopItems[4].Value,
                    }
                },
                {
                    1,
                    new List<ShopItem>()
                    {
                        shopItems[5].Value,
                        shopItems[6].Value,
                    }
                }
            };

            int pageNumber = 1;
            int maxPages = pages.Count;
            var items = Utils.LoadPage(pages, pageNumber - 1);

            #endregion

            Func<ShopItem, string> valueFunc = item => $"ID: {item.Id}";

            #region Repeated Parts

            // Save repeated parts
            const string title = "Shop";
            const string description = "Welcome to the shop, get some stuff to make you better than your competition!";

            string PREVIOUS_BUTTON_ID = $"previous-page-shop-{ctx.User.Id}";
            string NEXT_BUTTON_ID = $"next-page-shop-{ctx.User.Id}";

            const string PREVIOUS_BUTTON_LABEL = "Previous";
            const string NEXT_BUTTON_LABEL = "Next";

            var footer = new DiscordEmbedBuilder.EmbedFooter()
            {
                Text = $"View more info on any item by passing in the id argument! - Page {pageNumber}/{maxPages}"
            };

            var fields = Utils.MakeShopItemFields(items, valueFunc);

            #endregion

            var buttons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, pageNumber == 1, new DiscordComponentEmoji("◀️")),
                new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
            };

            var embed = Utils.MakeEmbed(DiscordColor.Cyan, title, description, footer: footer, fields: fields);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(buttons).AddEmbed(embed));

            #region Interaction Handler

            AsyncEventHandler<DiscordClient, ComponentInteractionCreateEventArgs> handlePagination = async (s, e) =>
            {
                if (e.User.Id != ctx.User.Id) return;

                if (e.Id == NEXT_BUTTON_ID)
                {
                    pageNumber += 1;
                    buttons = new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, pageNumber == 1, new DiscordComponentEmoji("◀️")),
                        new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
                    };
                    items = Utils.LoadPage(pages, pageNumber - 1);
                    fields = Utils.MakeShopItemFields(items, valueFunc);
                    footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = $"View more info on any item by passing in the id argument! - Page {pageNumber}/{maxPages}"
                    };
                    embed = Utils.MakeEmbed(DiscordColor.Cyan, title, description, footer: footer, fields: fields);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(buttons));
                }
                else if (e.Id == PREVIOUS_BUTTON_ID)
                {
                    pageNumber -= 1;
                    buttons = new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, pageNumber == 1, new DiscordComponentEmoji("◀️")),
                        new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
                    };
                    items = Utils.LoadPage(pages, pageNumber - 1);
                    fields = Utils.MakeShopItemFields(items, valueFunc);
                    footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = $"View more info on any item by passing in the id argument! - Page {pageNumber}/{maxPages}"
                    };
                    embed = Utils.MakeEmbed(DiscordColor.Cyan, title, description, footer: footer, fields: fields);

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed).AddComponents(buttons));
                }
            };

            #endregion

            ctx.Client.ComponentInteractionCreated += handlePagination;

            await Task.Delay(180 * 1000);

            ctx.Client.ComponentInteractionCreated -= handlePagination;

            embed = Utils.MakeEmbed(DiscordColor.Yellow, title, $"{description}\n\n**Interaction collector expired, please use this command again if you want to check out a different page.**", footer: footer,
                fields: fields);
            buttons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, true, new DiscordComponentEmoji("◀️")),
                new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, true, new DiscordComponentEmoji("▶️"))
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(buttons).AddEmbed(embed));
        }

        [SlashCommand("buy", "Buy an item from the shop")]
        public async Task BuyItemCommand(InteractionContext ctx, [Option("id", "The item ID to buy")] string id, [Option("quantity", "How many of this item to buy, defaults to 1")] long quantity = 1)
        {
            await ctx.DeferAsync(true);

            if (!Globals.Items.ContainsKey(id))
            {
                var noItemEmbed = Utils.MakeErrorEmbed($"No item with an ID of `{id}` was found.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noItemEmbed));
                return;
            }

            var user = await Utils.GetUserAsync(ctx.User);
            var item = Globals.Items[id];

            if (quantity > 1 && item.IsSinglePurchaseOnly)
            {
                var badQuantityEmbed = Utils.MakeErrorEmbed("You're buying an item that is limited to 1 purchase, but you provided a quantitiy greater than 1!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(badQuantityEmbed));
                return;
            }

            var result = await item.BuyAsync(user, Convert.ToInt32(quantity));
            ulong? price = quantity > 0 ? item.Price * Convert.ToUInt64(quantity) : null;

            if (!result.IsSuccess)
            {
                var description = $"Couldn't purchase **{Utils.ToLocaleString(quantity)}** of **{item.Name}**!";


                switch (result.FailureReason)
                {
                    case ItemPurchaseFailureReason.BadRequest:
                        description += " You have provided a quantity less than 1, this is not allowed.";
                        break;
                    case ItemPurchaseFailureReason.InsuffientFunds:
                        description += $" You don't have enough bones to purchase {Utils.ToLocaleString(quantity)} of this item.\n\n**Item requires:** `{Utils.ToLocaleString(Convert.ToUInt64(price))}` bones (with the quantity included)\n**You have:** `{Utils.ToLocaleString(user.AmountOfBonesCollected)}` bones\n**You need:** `{Utils.ToLocaleString(Convert.ToUInt64(price - user.AmountOfBonesCollected))}` more bones.\n\n**Please try again once you get enough to purchase this item.**";
                        break;
                }

                var errorEmbed = Utils.MakeErrorEmbed(description);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(errorEmbed));
                return;
            }

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, description: $"You have successfully purchased **{Utils.ToLocaleString(quantity)}** of **{item.Name}**, if this item is not an auto-redeem item, it has been added to your inventory.\n\n**You now have `{Utils.ToLocaleString(Convert.ToUInt64(user.AmountOfBonesCollected - price))}` bones left.**");
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }

        [SlashCommand("userinfo", "View your (or someone else's) information!")]
        public async Task UserInformationCommand(InteractionContext ctx, [Option("user", "A user to view the balance of, defaults to you.")] DiscordUser? discordUser = null)
        {
            var user = discordUser ?? ctx.User;

            var currentUser = await Utils.GetUserAsync(ctx.User);

            bool canSeeSecretFields = ctx.User.Id == user.Id || currentUser.Items.SecretRevealer > Convert.ToUInt64(0);
            await ctx.DeferAsync(canSeeSecretFields); // Hide the damn embed from the public if the user in context can see the SECRET fields, the public doesn't deserve to see that.

            var document = await Utils.GetUserAsync(user);

            if (ctx.User.Id != user.Id && currentUser.Items.SecretRevealer > 0)
            {
                await Utils.UpdateUserAsync(ctx.User, x => x.Items, new ItemAmounts()
                {
                    AcidSpit = currentUser.Items.AcidSpit,
                    DigestionMedicine = currentUser.Items.DigestionMedicine,
                    Lube = currentUser.Items.Lube,
                    RatPoison = currentUser.Items.RatPoison,
                    SecretRevealer = currentUser.Items.SecretRevealer - 1,
                    SuperCrystal = currentUser.Items.SuperCrystal
                });
            }

            #region Repeated Parts

            string BASE_TITLE = $"Info of {Utils.MakeFirstCharacterUppercase(user.Username)} ({user.Username})";
            const string SECRET_TEXT = "[SECRET]";

            string PREVIOUS_BUTTON_ID = $"previous-page-userinfo-{ctx.User.Id}-{user.Id}-{new Random().Next()}";
            const string PREVIOUS_BUTTON_LABEL = "Previous";

            string NEXT_BUTTON_ID = $"next-page-userinfo-{ctx.User.Id}-{user.Id}-{new Random().Next()}";
            const string NEXT_BUTTON_LABEL = "Next";

            #endregion

            #region Fields

            var page1Fields = new List<FieldData>()
            {
                new FieldData("Bones In Stomach", Utils.EvaluateBool(canSeeSecretFields, Utils.ToLocaleString(document.AmountOfBonesInStomach), SECRET_TEXT), true),
                new FieldData("Bones Collected", Utils.ToLocaleString(document.AmountOfBonesCollected), true)
            };

            // This will be updated every time a new item is added, until I find a way to automate it.
            var page2Fields = new List<FieldData>()
            {
                new FieldData("Acid Spit", Utils.ToLocaleString(document.Items.AcidSpit), true),
                new FieldData("Digestion Medicine", Utils.ToLocaleString(document.Items.DigestionMedicine), true),
                new FieldData("Lube", Utils.EvaluateBool(canSeeSecretFields, Utils.ToLocaleString(document.Items.Lube), SECRET_TEXT), true),
                new FieldData("Rat Poison", Utils.EvaluateBool(canSeeSecretFields, Utils.ToLocaleString(document.Items.RatPoison), SECRET_TEXT), true),
                new FieldData("Secret Revealer", Utils.EvaluateBool(canSeeSecretFields, Utils.ToLocaleString(document.Items.SecretRevealer), SECRET_TEXT), true),
                new FieldData("Super Crystal", Utils.EvaluateBool(canSeeSecretFields, Utils.ToLocaleString(document.Items.SuperCrystal), SECRET_TEXT), true)
            };

            var page3Fields = new List<FieldData>()
            {
                new FieldData("Amount Of People Inside", Utils.ToLocaleString(document.AmountOfPeopleInStomach), true),
                new FieldData("Capacity", Utils.ToLocaleString(document.StomachCapacity), true),
                new FieldData("Skin Color", document.SkinColor, true),
                new FieldData("Stomach Color", document.StomachColor, true),
                new FieldData("Acid Color", document.AcidColor, true)
            };

            #endregion

            var embeds = new Dictionary<uint, DiscordEmbed>()
            {
                {
                    0,
                    Utils.MakeEmbed(DiscordColor.Cyan, $"{BASE_TITLE} - Balance", fields: page1Fields)
                },
                {
                    1,
                    Utils.MakeEmbed(DiscordColor.Cyan, $"{BASE_TITLE} - Items", fields: page2Fields)
                },
                {
                    2,
                    Utils.MakeEmbed(DiscordColor.Cyan, $"{BASE_TITLE} - Stomach Stats", fields: page3Fields)
                }
            };

            uint pageNumber = 1;
            uint maxPages = (uint)embeds.Count;
            var embed = Utils.LoadPage(embeds, pageNumber - 1);

            var buttons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, pageNumber == 1, new DiscordComponentEmoji("◀️")),
                new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(buttons).AddEmbed(embed));

            #region Interaction Collector

            AsyncEventHandler<DiscordClient, ComponentInteractionCreateEventArgs> interactionCollector = async (s, e) =>
            {
                if (e.User.Id != ctx.User.Id) return;

                if (e.Id == NEXT_BUTTON_ID)
                {
                    pageNumber += 1;
                    embed = Utils.LoadPage(embeds, pageNumber - 1);
                    buttons = new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, pageNumber == 1, new DiscordComponentEmoji("◀️")),
                        new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
                    };

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddComponents(buttons).AddEmbed(embed));
                }
                else if (e.Id == PREVIOUS_BUTTON_ID)
                {
                    pageNumber -= 1;
                    embed = Utils.LoadPage(embeds, pageNumber - 1);
                    buttons = new DiscordButtonComponent[]
                    {
                        new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, pageNumber == 1, new DiscordComponentEmoji("◀️")),
                        new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, pageNumber == maxPages, new DiscordComponentEmoji("▶️"))
                    };

                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddComponents(buttons).AddEmbed(embed));
                }
            };

            #endregion

            ctx.Client.ComponentInteractionCreated += interactionCollector;

            await Task.Delay(180 * 1000);

            var finalFields = new List<FieldData>();

            foreach (var field in embed.Fields) finalFields.Add(new FieldData(field.Name, field.Value, field.Inline));

            embed = Utils.MakeEmbed(DiscordColor.Yellow, embed.Title, "This interaction collector has expired, please run this command again to see another page.", fields: finalFields);

            buttons = new DiscordButtonComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Danger, PREVIOUS_BUTTON_ID, PREVIOUS_BUTTON_LABEL, true, new DiscordComponentEmoji("◀️")),
                new DiscordButtonComponent(ButtonStyle.Success, NEXT_BUTTON_ID, NEXT_BUTTON_LABEL, true, new DiscordComponentEmoji("▶️"))
            };

            ctx.Client.ComponentInteractionCreated -= interactionCollector;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddComponents(buttons).AddEmbed(embed));
        }

        [SlashCommand("escape", "Attempt to escape from your captor.")]
        public async Task EscapeCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            var user = await Utils.GetUserAsync(ctx.User);

            if (!user.IsInStomach)
            {
                var word1 = Utils.LanguageFilter(ctx.Guild, "flip", "fuck");
                var word2 = Utils.LanguageFilter(ctx.Guild, "flipping", "fucking");
                var embed = Utils.MakeErrorEmbed($"Okay, for {word2} real, what the {word1} are you trying to escape from? {Utils.MakeFirstCharacterUppercase(word2)} reality?!");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            User captor = (await Utils.GetCaptorAsync(user))!;
            DiscordMember captorDiscord = await ctx.Guild.GetMemberAsync(Convert.ToUInt64(captor.Id));

            var random = new Random();
            double randomDouble = random.NextDouble();

            bool hasLube = user.Items.Lube > 0;
            bool success = Utils.EvaluateBool(hasLube, randomDouble > 0.55, randomDouble > 0.75);

            if (hasLube)
            {
                ItemAmounts itemAmounts = new()
                {
                    AcidSpit = user.Items.AcidSpit,
                    DigestionMedicine = user.Items.DigestionMedicine,
                    Lube = user.Items.Lube - 1,
                    RatPoison = user.Items.RatPoison,
                    SecretRevealer = user.Items.SecretRevealer
                };
                await Utils.UpdateUserAsync(ctx.User, x => x.Items, itemAmounts);
            }

            if (success)
            {
                var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Success", $"You successfully escaped from {captorDiscord.Mention}'s stomach!");

                var peopleInCaptor = captor.PeopleInStomach;
                peopleInCaptor.Remove(user.Id);

                IEnumerable<object> currentUserValues = new object[] { false, 0f };
                IEnumerable<object> captorUserValues = new object[] { peopleInCaptor, Convert.ToUInt32(peopleInCaptor.Count) };

                await Utils.UpdateUserAsync(ctx.User, new List<Expression<Func<User, object>>>()
                {
                    x => x.IsInStomach,
                    x => x.Softness
                }, currentUserValues);
                await Utils.UpdateUserAsync(captorDiscord, new List<Expression<Func<User, object>>>()
                {
                    x => x.PeopleInStomach,
                    x => x.AmountOfPeopleInStomach
                }, captorUserValues);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
            }
            else
            {
                var failureEmbed = Utils.MakeEmbed(DiscordColor.Red, "Failure", $"You failed to escape from {captorDiscord.Mention}'s stomach{Utils.EvaluateBool(user.CanBeDmed, ", and now they know you tried to escape!", "however, they're oblivious!")}");
                var captorEmbed = Utils.MakeEmbed(DiscordColor.Yellow, "Attempted Escape", $"{ctx.User.Mention} tried to escape from your stomach, so... you gonna punish them or something...?");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(failureEmbed));

                if (captor.CanBeDmed)
                {
                    await captorDiscord.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(captorEmbed));
                }
            }
        }

        [SlashCommand("daily", "Claim your daily reward!")]
        [SlashCooldown(1, 86400.0, SlashCooldownBucketType.User)]
        public async Task DailyCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var user = await Utils.GetUserAsync(ctx.User);
            var amount = user.AmountOfBonesCollected + Globals.DailyAmount;

            await Utils.UpdateUserAsync(ctx.User, x => x.AmountOfBonesCollected, amount);
            var desc = $"You claimed your daily reward of **{Utils.ToLocaleString(Globals.DailyAmount)}** bones! You now have **{Utils.ToLocaleString(amount)}** bones.";

            var successEmbed = Utils.MakeEmbed(DiscordColor.Green, "Daily claimed!", desc);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(successEmbed));
        }
    }
}
