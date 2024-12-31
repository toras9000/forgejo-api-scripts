#r "nuget: ForgejoApiClient, 9.0.0-rev.3"
#r "nuget: Lestaly, 0.69.0"
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
};

var noInteract = Args.Any(a => a == "--no-interact");
var pauseMode = noInteract ? PavedPause.None : PavedPause.Any;

return await Paved.RunAsync(config: c => c.PauseOn(pauseMode), action: async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    WriteLine("APIトークンを読み込み ...");
    var forgejoToken = await settings.ApiKeyFile.ScriptScrambler().LoadTokenAsync() ?? throw new Exception("トークン情報を読み取れない");
    if (forgejoToken.Service.AbsoluteUri != settings.ServiceURL.AbsoluteUri) throw new Exception("保存情報が対象と合わない");

    WriteLine("クライアント準備 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var me = default(User);
    using (var breaker = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
    {
        // 初期化直後はAPI呼び出しがエラーとなることがあるようなので、一定時間繰り返し呼び出しを試みる。
        while (me == null || me.login == null)
        {
            try { me = await forgejo.User.GetMeAsync(signal.Token); }
            catch { await Task.Delay(500); }
        }
    }

    WriteLine("ユーザリポジトリの作成 ...");
    var users = await forgejo.Admin.ListUsersAsync(cancelToken: signal.Token);
    foreach (var user in users)
    {
        if (user.login == null) continue;
        var owner = user.login;
        WriteLine($"  Create for {Chalk.Blue[owner]}");
        try
        {
            var name = $"repo-{Guid.NewGuid()}";
            var repo = await forgejo.Admin.CreateUserRepoAsync(owner, new(name), signal.Token);
            WriteLine(Chalk.Green[$"    作成 .. {name}"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"    {ex.Message}"]);
        }
    }

    WriteLine("組織リポジトリの作成 ...");
    var orgs = await forgejo.Admin.ListOrganizationsAsync(cancelToken: signal.Token);
    foreach (var org in orgs)
    {
        if (org.username == null) continue;
        var owner = org.username;
        WriteLine($"  Create for {Chalk.Blue[owner]}");
        try
        {
            var name = $"repo-{Guid.NewGuid()}";
            var repo = await forgejo.Admin.CreateUserRepoAsync(owner, new(name), signal.Token);
            WriteLine(Chalk.Green[$"    作成 .. {name}"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"    {ex.Message}"]);
        }
    }
});
