#r "nuget: ForgejoApiClient, 11.0.0-rev.1"
#r "nuget: Lestaly, 0.79.0"
#r "nuget: Kokuban, 0.2.0"
#load "../.env-helper.csx"
#nullable enable
using System.Threading;
using ForgejoApiClient;
using ForgejoApiClient.Api;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // サービスのURL
    ServiceURL = new Uri("http://localhost:9940"),

    // トークン保存ファイル
    ApiKeyFile = ThisSource.RelativeFile("../.auth-forgejo-api"),

    // 作成するユーザの定義
    Users = new CreateUserOption[]
    {
        new(username: "test-user", email: "test@example.com", password: "test-pass", full_name: "テストユーザ"),
        new(username: "toras9000", email: "toras9000@example.com", password: "toras9000-pass", full_name: "toras9000"),
    },

    // 作成する組織の定義
    Organizations = new CreateOrgOption[]
    {
        new(username: "test-org", full_name: "テスト組織"),
    },

    // 作成するチームの定義
    Teams = new TeamDefine[]
    {
        new("test-org", new(name: "test-team1", units_map: new Dictionary<string, string>()
        {
            ["repo.code"] = "write",
            ["repo.ext_issues"] = "write",
            ["repo.ext_wiki"] = "write",
            ["repo.issues"] = "write",
            ["repo.projects"] = "write",
            ["repo.pulls"] = "write",
            ["repo.releases"] = "write",
            ["repo.wiki"] = "write",
        })),
    },
};

record TeamDefine(string Org, CreateTeamOption Options);

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-interact"), async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    WriteLine("APIトークンを読み込み ...");
    var forgejoToken = await settings.ApiKeyFile.ScriptScrambler().LoadTokenAsync() ?? throw new Exception("トークン情報を読み取れない");
    if (forgejoToken.Service.AbsoluteUri != settings.ServiceURL.AbsoluteUri) throw new Exception("保存情報が対象と合わない");

    WriteLine("クライアント準備 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var me = default(User);
    using (var breaker = signal.Token.CreateLink(TimeSpan.FromSeconds(5)))
    {
        // 初期化直後はAPI呼び出しがエラーとなることがあるようなので、一定時間繰り返し呼び出しを試みる。
        while (me?.login == null)
        {
            try { me = await forgejo.User.GetMeAsync(breaker.Token); }
            catch { await Task.Delay(TimeSpan.FromMilliseconds(500), breaker.Token); }
        }
    }

    WriteLine("ユーザの作成 ...");
    foreach (var options in settings.Users)
    {
        WriteLine($"  CreateUser: {Chalk.Blue[options.username]}");
        Write("  .. ");
        try
        {
            var user = await forgejo.Admin.CreateUserAsync(options, signal.Token);
            WriteLine(Chalk.Green["作成"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[ex.Message]);
        }
    }

    WriteLine("組織の作成 ...");
    foreach (var options in settings.Organizations)
    {
        WriteLine($"  CreateOrg: {Chalk.Blue[options.username]}");
        Write("  .. ");
        try
        {
            var user = await forgejo.Admin.CreateOrganizationAsync(me.login, options, signal.Token);
            WriteLine(Chalk.Green["作成"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[ex.Message]);
        }
    }

    WriteLine("チームの作成 ...");
    foreach (var define in settings.Teams)
    {
        WriteLine($"  CreateTeam: {Chalk.Blue[define.Org]} - {Chalk.Blue[define.Options.name]}");
        Write("  .. ");
        try
        {
            var user = await forgejo.Organization.CreateTeamAsync(define.Org, define.Options, signal.Token);
            WriteLine(Chalk.Green["作成"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[ex.Message]);
        }
    }
});
