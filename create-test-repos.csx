#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 13.0.0-rev.1"
#r "nuget: R3, 1.3.0"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.112.0"
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

    // テストリポジトリ
    TestRepos = new[]
    {
        ThisSource.RelativeDirectory("./test-repos/show-vars"),
        ThisSource.RelativeDirectory("./test-repos/schedule"),
    },
};

// メイン処理
return await Paved.ProceedAsync(async () =>
{
    // コンソール準備
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("テストリポジトを登録する");
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

    WriteLine("リポジトリ作成 ...");
    foreach (var repoDir in settings.TestRepos)
    {
        var repoPath = $"{apiUser.login}/{repoDir.Name}";
        WriteLine($"  {repoPath}");
        var repos = await forgejo.Repository.SearchAsync(q: repoPath, cancelToken: signal.Token);
        if (repos.data?.Any(r => r.full_name == repoPath) == true)
        {
            WriteLine(Chalk.Gray[$"  .. 既に存在する"]);
            continue;
        }
        var repo = await forgejo.Repository.CreateAsync(new(name: repoDir.Name), cancelToken: signal.Token);
        WriteLine(Chalk.Green["  .. 完了"]);

        WriteLine("  .. ファイルの登録");
        var files = new List<ChangeFileOperation>();
        foreach (var file in repoDir.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var relPath = file.RelativePathFrom(repoDir, ignoreCase: true).Replace('\\', '/');
            var content = Convert.ToBase64String(file.ReadAllBytes());
            files.Add(new(ChangeFileOperationOperation.Create, path: relPath, content));
        }
        await forgejo.Repository.UpdateFilesAsync(apiUser.login, repoDir.Name, new(message: repoDir.Name, files: files), cancelToken: signal.Token);
        WriteLine(Chalk.Green["  .. 完了"]);

        WriteLine("  .. ブランチを作成 ...");
        await forgejo.Repository.CreateBranchAsync(apiUser.login, repoDir.Name, new(new_branch_name: "v0.0.0"), cancelToken: signal.Token);
        WriteLine(Chalk.Green["  .. 完了"]);
    }

});
