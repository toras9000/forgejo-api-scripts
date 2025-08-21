#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 12.0.1-rev.4"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.102.0"
#load ".env-helper.csx"
#load ".forgejo-helper.csx"
#nullable enable
using ForgejoApiClient;
using ForgejoApiClient.Api;
using Kokuban;
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

    // リポジトリ更新パラメータ
    RepoSettings = new EditRepoOption(
        has_issues: false,
        has_pull_requests: false,
        has_packages: false,
        has_projects: false,
        has_wiki: false,
        has_actions: true
    ),
};

// メイン処理
return await Paved.ProceedAsync(async () =>
{
    // コンソール準備
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("全てのリポジトリ設定を更新する");
    WriteLine($"  Forgejo   : {settings.Forgejo.ServiceURL}");
    WriteLine();

    WriteLine("サービス認証情報の準備");
    var forgejoToken = await settings.Forgejo.ApiTokenFile.BindTokenAsync("Forgejo APIトークン", settings.Forgejo.ServiceURL, signal.Token);
    if (forgejoToken.Service.AbsoluteUri != settings.Forgejo.ServiceURL.AbsoluteUri) throw new Exception("保存情報が対象と合わない");

    WriteLine("Forgejo クライアントの生成 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var apiUser = await forgejo.User.GetMeAsync(cancelToken: signal.Token);
    if (apiUser.login == null) throw new PavedMessageException("情報取得失敗", PavedMessageKind.Error);
    WriteLine(Chalk.Gray[$"  .. User: {apiUser.login}"]);
    WriteLine();

    WriteLine("各リポジトリ設定の更新");
    await foreach (var repo in forgejo.AllReposAsync(signal.Token).WithCancellation(signal.Token))
    {
        WriteLine($"Update: {repo.full_name}");
        await forgejo.Repository.UpdateAsync(repo.owner!.login!, repo.name!, settings.RepoSettings);
    }
});
