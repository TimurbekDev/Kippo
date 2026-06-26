using Kippo.Attribute;
using Kippo.Contexs;
using Kippo.Handlers;
using Kippo.Keyboard;
using KippoGramm;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;

public class MyHandler : BotUpdateHandler
{
    [Command("start")]
    public async Task Start(Context context, IUserService userService)
    {
        await userService.SetUserName(2,"Hello");

        var namee = await userService.GetUserName(2);

        var keyboard = ReplyKeyboardBuilder.Create()
            .Button("📝 Register")
            .Button("ℹ️ Info")
            .Row()
            .Button("❓ Help")
            .Resize()
            .Build();

        await context.Reply(
            "🤖 *Welcome to Kippo Demo Bot!*\n\n" +
            "Choose an option to get started:",
            keyboard
        );
    }

    [Command("help")]
    [Text(Pattern = "❓ Help")]
    public async Task Help(Context context)
    {
        await context.Reply(
            "📚 *Available Commands*\n\n" +
            "/start - Show main menu\n" +
            "/help - Show this message\n" +
            "/register - Start registration\n" +
            "/info - Show your info\n" +
            "/menu - Show inline menu"
        );
    }

    [Command("register")]
    [Text(Pattern = "📝 Register")]
    public async Task StartRegistration(Context context)
    {
        context.Session!.State = "ask_age";

        var keyboard = ReplyKeyboardBuilder.Create()
            .Button("Cancel ❌")
            .Resize()
            .OneTime()
            .Build();

        await context.Reply(
            "👤 *Let's get you registered!*\n\n" +
            "Please enter your age:",
            keyboard
        );
    }

    [Text(State = "ask_age")]
    public async Task AskAge(Context context)
    {
        var text = context.Message.Text;

        // Handle cancel
        if (text == "Cancel ❌")
        {
            await Cancel(context);
            return;
        }

        if (!int.TryParse(text, out var age) || age < 13 || age > 120)
        {
            await context.Reply("❌ Please enter a valid age (13-120).");
            return;
        }

        context.Session!.Data["age"] = age;
        context.Session!.State = "ask_name";

        await context.Reply("✅ Great! Now, what's your name?");
    }

    [Text(State = "ask_name")]
    public async Task AskName(Context context)
    {
        var name = context.Message.Text;

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            await context.Reply("❌ Please enter a valid name (at least 2 characters).");
            return;
        }

        context.Session!.Data["name"] = name;
        context.Session!.State = "ask_country";

        var keyboard = InlineKeyboardBuilder.Create()
            .Button("🇺🇸 USA", "country_usa")
            .Button("🇬🇧 UK", "country_uk")
            .Row()
            .Button("🇩🇪 Germany", "country_de")
            .Button("🇫🇷 France", "country_fr")
            .Row()
            .Button("🌍 Other", "country_other")
            .Build();

        await context.Reply(
            $"Nice to meet you, {name}! 👋\n\n" +
            "Where are you from?",
            keyboard
        );
    }

    [CallbackQuery("country_*")]
    public async Task HandleCountry(Context context)
    {
        var country = context.Update.CallbackQuery!.Data!.Replace("country_", "").ToUpper();
        var countryName = country switch
        {
            "USA" => "🇺🇸 United States",
            "UK" => "🇬🇧 United Kingdom",
            "DE" => "🇩🇪 Germany",
            "FR" => "🇫🇷 France",
            _ => "🌍 Other"
        };

        context.Session!.Data["country"] = countryName;
        context.Session!.State = null; // Registration complete

        var name = context.Session!.Data["name"];
        var age = context.Session!.Data["age"];

        // Answer the callback query to remove loading state
        await context.Callback.Answer();

        var keyboard = ReplyKeyboardBuilder.Create()
            .Button("📝 Register")
            .Button("ℹ️ Info")
            .Row()
            .Button("❓ Help")
            .Resize()
            .Build();

        await context.Reply(
            "🎉 *Registration Complete!*\n\n" +
            $"📋 Your Info:\n" +
            $"• Name: {name}\n" +
            $"• Age: {age}\n" +
            $"• Country: {countryName}",
            keyboard
        );
    }

    [Command("info")]
    [Text(Pattern = "ℹ️ Info")]
    public async Task ShowInfo(Context context, IUserService userService)
    {
        var name2 = await userService.GetUserName(2);
        if (context.Session?.Data.ContainsKey("name") != true)
        {
            await context.Reply(
                "❌ You haven't registered yet!\n\n" +
                $"Use /register to get started."
            );
            return;
        }

        var name = context.Session!.Data["name"];
        var age = context.Session!.Data["age"];
        var country = context.Session!.Data.GetValueOrDefault("country", "Not set");

        await context.Reply(
            "📋 *Your Information*\n\n" +
            $"• Name: {name}\n" +
            $"• Age: {age}\n" +
            $"• Country: {country}"
        );
    }

    [Command("menu")]
    public async Task ShowInlineMenu(Context context)
    {
        var keyboard = InlineKeyboardBuilder.Create()
            .Button("✅ Option 1", "opt_1")
            .Button("✅ Option 2", "opt_2")
            .Row()
            .Button("⚙️ Settings", "settings")
            .Row()
            .UrlButton("📖 Documentation", "https://github.com/yourusername/KippoGramm")
            .Build();

        await context.Reply(
            "🔘 *Inline Menu*\n\n" +
            "Choose an option:",
            keyboard
        );
    }

    [CallbackQuery("opt_*")]
    public async Task HandleOption(Context context)
    {
        var option = context.Update.CallbackQuery!.Data!.Replace("opt_", "");
        await context.Callback.Answer($"You selected Option {option}");
        await context.Reply($"✅ You selected Option {option}!");
    }

    [CallbackQuery("settings")]
    public async Task HandleSettings(Context context)
    {
        var keyboard = InlineKeyboardBuilder.Create()
            .Button("🔔 Notifications", "set_notif")
            .Button("🌐 Language", "set_lang")
            .Row()
            .Button("🔙 Back", "back_menu")
            .Build();

        await context.Reply(
            "⚙️ *Settings*\n\n" +
            "Configure your preferences:",
            keyboard
        );
    }

    [CallbackQuery("back_menu")]
    public async Task BackToMenu(Context context)
    {
        await ShowInlineMenu(context);
    }

    [Text(Pattern = "Cancel ❌")]
    public async Task Cancel(Context context)
    {
        context.Session!.State = null;
        context.Session!.Data.Clear();

        var keyboard = ReplyKeyboardBuilder.Create()
            .Button("📝 Register")
            .Button("ℹ️ Info")
            .Row()
            .Button("❓ Help")
            .Resize()
            .Build();

        await context.Reply("❌ Operation cancelled.", keyboard);
    }

    [Text]
    public async Task GlobalTextHandler(Context context)
    {
        await context.Reply(
            $"📝 You said: _{context.Message.Text}_\n\n" +
            "Use /help to see available commands."
        );
    }

    [Contact]
    public async Task OnContact(Context context, Contact contact)
    {
        await context.Reply(
            $"📞 Contact qabul qilindi!\n\n" +
            $"• Ism: {contact.FirstName}\n" +
            $"• Telefon: {contact.PhoneNumber}"
        );
    }

    [ChatMember]
    public async Task OnNewMember(Context context)
    {
        Console.WriteLine("🔔 [ChatMember] handler called!");

        var chatMember = context.Update.ChatMember ?? context.Update.MyChatMember;

        Console.WriteLine($"ChatMember: {context.Update.ChatMember != null}, MyChatMember: {context.Update.MyChatMember != null}");

        if (chatMember == null)
        {
            Console.WriteLine("❌ chatMember is null!");
            return;
        }

        var newMember = chatMember.NewChatMember;
        var oldStatus = chatMember.OldChatMember.Status;
        var newStatus = newMember.Status;

        Console.WriteLine($"👤 User: {newMember.User.FirstName}, Old Status: {oldStatus}, New Status: {newStatus}");

        if ((oldStatus == ChatMemberStatus.Left || oldStatus == ChatMemberStatus.Kicked) &&
            (newStatus == ChatMemberStatus.Member || newStatus == ChatMemberStatus.Administrator || 
             newStatus == ChatMemberStatus.Creator || newStatus == ChatMemberStatus.Restricted))
        {
            var user = newMember.User;
            var isBot = user.IsBot;

            var greeting = isBot 
                ? $"🤖 Bot qo'shildi: @{user.Username ?? user.FirstName}\n⚠️ Botlar avtomatik tekshiriladi!"
                : $"👋 Xush kelibsiz, {user.FirstName}!\n\nGruppaga qo'shilganingiz uchun rahmat!";

            Console.WriteLine($"📤 Sending greeting message...");

            await context.BotClient.SendMessage(
                chatId: chatMember.Chat.Id,
                text: greeting,
                cancellationToken: context.CancellationToken
            );

            Console.WriteLine($"✅ Greeting message sent!");
        }
        else
        {
            Console.WriteLine($"⚠️ Status change not matched for greeting. Old: {oldStatus} -> New: {newStatus}");
        }
    }
}
