#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 13.0.0-rev.1"
#r "nuget: Lestaly.General, 0.112.0"
#r "nuget: Kokuban, 0.2.0"
#load "../.env-helper.csx"
#nullable enable
using System.Threading;
using ForgejoApiClient;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // Forgejo 関連の情報
    Forgejo = new
    {
        // サービスURL
        ServiceURL = new Uri("http://localhost:9940/"),

        // トークン保存ファイル
        AdminApiKeyFile = ThisSource.RelativeFile("../.admin-forgejo.key"),
    },

    // ランナー関連の情報
    Runner = new
    {
        // composeファイル
        ComposeFile = ThisSource.RelativeFile("./docker/compose.yml"),

        // コンテナ サービス名
        ServiceName = "runner",

        // 登録先インスタンス (for runnner)
        RegisterInstance = "http://forgejo-app-container:3000",

        // 登録種別 ("global" or "user")
        RegisterType = "global",

        // 登録名
        RegisterName = "default-runner",
    },
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-interact"), async () =>
{
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("APIトークンを読み込み ...");
    var forgejoToken = await settings.Forgejo.AdminApiKeyFile.ScriptScrambler().LoadTokenAsync(signal.Token) ?? throw new Exception("トークン情報を読み取れない");
    if (forgejoToken.Service.AbsoluteUri != settings.Forgejo.ServiceURL.AbsoluteUri) throw new Exception("保存情報が対象と合わない");

    WriteLine("Forgejo クライアントの生成 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var apiUser = await forgejo.User.GetMeAsync(cancelToken: signal.Token);
    if (apiUser.login == null) throw new PavedMessageException("情報取得失敗", PavedMessageKind.Error);
    WriteLine(Chalk.Gray[$"  .. User: {apiUser.login}"]);
    WriteLine();

    WriteLine("ランナー登録トークンを取得");
    WriteLine($".. Type: {settings.Runner.RegisterType}");
    var runnerToken = default(string);
    if (settings.Runner.RegisterType == "global")
    {
        if (apiUser.is_admin != true) throw new PavedMessageException("APIトークンのユーザが管理者ではない。", PavedMessageKind.Error);
        runnerToken = (await forgejo.Admin.GetActionsRunnerRegistrationTokenAsync(signal.Token)).token;
    }
    else if (settings.Runner.RegisterType == "user")
    {
        runnerToken = (await forgejo.User.GetActionsRunnerRegistrationTokenAsync(signal.Token)).token;
    }
    else
    {
        throw new PavedMessageException("登録種別設定不正", PavedMessageKind.Error);
    }
    WriteLine($".. Token: {runnerToken}");

    WriteLine("ランナー登録");
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(signal.Token);
    timeout.CancelAfter(TimeSpan.FromSeconds(10));
    await "docker".args("compose", "--file", settings.Runner.ComposeFile,
        "exec", settings.Runner.ServiceName,
        "forgejo-runner", "register",
            "--no-interactive",
            "--name", settings.Runner.RegisterName,
            "--instance", settings.Runner.RegisterInstance,
            "--token", runnerToken
    ).killby(timeout.Token).result().success();

    WriteLine(Chalk.Green[".. 完了"]);
});
