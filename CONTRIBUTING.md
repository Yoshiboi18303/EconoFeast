# EconoFeast Contribution Information

Here you'll learn how to contribute to EconoFeast. We'll go over how to get started.

## Getting Started

Let's start with getting started with contributions!

### Prerequisites

You'll need to have the following installed:

- [Git](https://git-scm.com/)
- [.NET SDK v7](https://dotnet.microsoft.com/en-us/download)
- A code editor or IDE of your choice _(I use **[Visual Studio](https://visualstudio.microsoft.com/downloads/)** personally)_

#### If using Visual Studio Code

If you're using Visual Studio Code, you'll need to install the following extension:

- **[C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)** _(to have a more **`Visual Studio`**-like experience)_

**OR:**

- **[C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)** _(for basic C# support)_

### Forking the Repository

First, you'll need to fork the repository.

Open a terminal and run the following command:

```bash
git clone https://github.com/Yoshiboi18303/EconoFeast.git
```

If you're using Visual Studio, you can also use the built-in Git tools to clone the repository.

Open Visual Studio and click on **`Clone a repository`**.

![Clone a repository](https://cdn.discordapp.com/attachments/1028104425371340851/1152380826798018650/vs.png)

You should then see a window like this:

![Clone a repository window](https://cdn.discordapp.com/attachments/1028104425371340851/1152380827041284116/image.png)

Paste the repository URL into the **`Repository location`** field, feel free to change the path field where you want the repository to be cloned to, and then click **`Clone`**.

Visual Studio will then clone the repository for you, as well as open it.

### Initial Setup

If you're using Visual Studio, it'll automatically restore the NuGet packages for you.

However, if you're using Visual Studio Code, you'll need to restore the NuGet packages yourself.

Open a terminal and run the following command:

```bash
dotnet restore
```

After restoration _(in either Visual Studio or Visual Studio Code)_, create a file called **`config.json`** in the **`EconoFeast`** folder/project.

Put this content into that file:

```json
{
  "token": "string",
  "supabase_url": "string",
  "supabase_key": "string",
  "radarcord_key": "string",
  "trello_key": "string",
  "trello_token": "string",
  "item_prices": [array of {"id": "string", "price": number}],
  "opt_out_users": [array of numbers],
  "vcodes_key": "string"
}
```

At the very least, replace the **`token`** field with your bot token as well as `supabase_url` and `supabase_key` with your Supabase URL and Supabase key respectively.

You will also have to replace `item_prices` with the item prices for your server.

### Running the Bot

If you're using Visual Studio, you can just click on the **`Start`** button to run the bot.

If you're using Visual Studio Code, you'll need to run the following command in a terminal:

```bash
dotnet run
```

## Making Changes

Now that you've got the bot running, you can start making changes!

Let's go over the hierarchy of the solution and projects.

```txt
EconoFeast
├── EconoFeast
|	├── Attributes
|	|──── GuildOnlyAttribute.cs
│   ├── Commands
|   |──── ConfigCommands.cs
|   |──── EconomyCommands.cs
|   |──── GuildOnlyCommands.cs
|   |──── InfoCommands.cs
|   |──── OtherCommands.cs
|   |──── OwnerOnlyCommands.cs
|   |── BOT-ADD-MESSAGE.md
|   |── config.json
|   |── Globals.cs
|   |── Logger.cs
|   |── Models.cs
|   |── Program.cs
|   |── ShopItem.cs
|   |── Utils.cs
```

The first **EconoFeast** folder is the solution folder, while the second one inside of it is the project folder.

Inside of the project folder, you'll see a bunch of folders and files. Let's go over what each of them are.

### Attributes

This folder contains all of the custom attributes that are used in the bot.

### Commands

This folder contains all of the command modules, which hold separated slash commands.

### BOT-ADD-MESSAGE.md

This file holds the message for when the bot is added to a guild.
