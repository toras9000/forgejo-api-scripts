#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 13.0.0-rev.1"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.106.0"
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
        has_actions: false
    ),
};

// メイン処理
return await Paved.ProceedAsync(async () =>
{
    // コンソール準備
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("リポジトを一覧する");
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

    WriteLine("リポジトリ情報の取得");
    await foreach (var repo in forgejo.AllReposAsync(signal.Token).WithCancellation(signal.Token))
    {
        WriteLine($"{repo.full_name}\tSize={repo.size?.ToHumanize()}iB\tIssues={repo.open_issues_count}\tPRs={repo.open_pr_counter}");
    }
});
