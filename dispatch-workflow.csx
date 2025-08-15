#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 12.0.1-rev.3"
#r "nuget: R3, 1.3.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.102.0"
#load ".env-helper.csx"
#nullable enable
using ForgejoApiClient;
using ForgejoApiClient.Api;
using Kokuban;
using R3;
using Lestaly;
using Lestaly.Cx;

// 設定
var settings = new
{
    // Forgejo 関連の情報
    Forgejo = new
    {
        // サービスURL
        ServiceURL = new Uri("http://localhost:9940/"),

        // APIキー保存ファイル
        ApiTokenFile = ThisSource.RelativeFile(".toras-forgejo.key"),
    },
};

// メイン処理
return await Paved.ProceedAsync(async () =>
{
    // コンソール準備
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // タイトル出力
    WriteLine("Workflowをトリガする");
    WriteLine($"  Forgejo   : {settings.Forgejo.ServiceURL}");
    WriteLine();

    // 認証情報を準備
    WriteLine("サービス認証情報の準備");
    var forgejoToken = await settings.Forgejo.ApiTokenFile.BindTokenAsync("Forgejo APIトークン", settings.Forgejo.ServiceURL, signal.Token);

    // APIクライアントを準備
    WriteLine("Forgejo クライアントの生成 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var apiUser = await forgejo.User.GetMeAsync(cancelToken: signal.Token);
    if (apiUser.login == null) throw new PavedMessageException("情報取得失敗", PavedMessageKind.Error);
    WriteLine(Chalk.Gray[$"  .. User: {apiUser.login}"]);
    WriteLine();

    // リポジトリ設定更新
    WriteLine(" ...");
    await forgejo.Repository.DispatchActionsWorkflowAsync(apiUser.login, "show-vars", "vars.yml", new("main"), signal.Token);

    var runs = await forgejo.Repository.ListActionsRunsAsync(apiUser.login, "show-vars", @event: ["workflow_dispatch"], status: ["waiting"]);
    runs = null!;
});
