# Frequently Asked Questions

<!--TOC-->
  - [The bot stops responding after I run the `eat` command, why?](#the-bot-stops-responding-after-i-run-the-eat-command-why)
  - [Will there be a tutorial?](#will-there-be-a-tutorial)
  - [Can I opt-out?](#can-i-opt-out)
<!--/TOC-->

## The bot stops responding after I run the `eat` command, why?

This is due to the `SlashCooldown` attribute on the `EatUserCommand` method, this is intentional until I can do some `DateTime` operators, so... just wait exactly 3 minutes after running the command, then you should be able to run it again.

This also happens with the `daily` command, but you have to wait 24 hours for that one.

This is reset when I restart the bot _(which I do often while testing new stuff)_.

Eventually, I'll have a testing bot ready for testing new stuff, so I won't have to restart the main bot as often, but that'll come when I can host the main bot.

## Will there be a tutorial?

Yes, the site will be coming soon with a tutorial page.

I'm deciding on what to use for it, I'm thinking of using **[Angular](https://angular.io/)**, but I'm not sure yet.

## Can I opt-out?

If you're a user, yes. Just contact a developer and they'll opt you out.

If you're a guild owner, no. Not yet, at least. I'm thinking of a way to opt-out guilds, but it's not ready yet.

