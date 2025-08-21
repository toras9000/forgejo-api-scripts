#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 12.0.1-rev.4"
#r "nuget: R3, 1.3.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Kurukuru, 1.5.0"
#r "nuget: Lestaly.General, 0.102.0"
#load ".env-helper.csx"
#nullable enable
using ForgejoApiClient;
using ForgejoApiClient.Api;
using Kokuban;
using R3;
using Lestaly;
using Lestaly.Cx;
using Kurukuru;
using System.Threading;

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

    WriteLine("Workflowをトリガする");
    WriteLine($"  Forgejo   : {settings.Forgejo.ServiceURL}");
    WriteLine();

    WriteLine("サービス認証情報の準備");
    var forgejoToken = await settings.Forgejo.ApiTokenFile.BindTokenAsync("Forgejo APIトークン", settings.Forgejo.ServiceURL, signal.Token);

    WriteLine("Forgejo クライアントの生成 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var apiUser = await forgejo.User.GetMeAsync(cancelToken: signal.Token);
    if (apiUser.login == null) throw new PavedMessageException("情報取得失敗", PavedMessageKind.Error);
    WriteLine(Chalk.Gray[$"  .. User: {apiUser.login}"]);
    WriteLine();

    WriteLine("ワークフロー実行 ...");
    var run = await forgejo.Repository.DispatchActionsWorkflowAsync(apiUser.login, "show-vars", "vars.yml", new(@ref: "main", return_run_info: true), signal.Token);
    if (run?.id == null) throw new PavedMessageException("実行情報取得失敗", PavedMessageKind.Warning);

    var runInfo = await Spinner.StartAsync("完了待機 ...", action: async (spinner) =>
    {
        var caption = spinner.Text;
        using var breaker = CancellationTokenSource.CreateLinkedTokenSource(signal.Token);
        breaker.CancelAfter(TimeSpan.FromMinutes(3));
        while (true)
        {
            var runInfo = await forgejo.Repository.GetActionsRunAsync(apiUser.login, "show-vars", run.id.Value, breaker.Token);
            spinner.Text = $"{caption} {runInfo.status} {runInfo.duration / 1000 / 1000 / 1000}";
            if (DateTimeOffset.UnixEpoch < runInfo.stopped) return runInfo;
            await Task.Delay(TimeSpan.FromSeconds(5), breaker.Token);
        }
    });

    WriteLine($"ワークフロー完了");
});
