#r "nuget: ForgejoApiClient, 9.0.0-rev.3"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly, 0.69.0"
#load ".env-helper.csx"
#load ".forgejo-helper.csx"
#nullable enable
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
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
        ApiTokenFile = ThisSource.RelativeFile(".auth-forgejo-api"),
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
return await Paved.RunAsync(config: c => c.AnyPause(), action: async () =>
{
    // コンソール準備
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    // タイトル出力
    void WriteScriptTitle()
    {
        const string ScriptTitle = "Forgejoの全てのリポジトリ設定を更新する";
        WriteLine(ScriptTitle);
        WriteLine($"  Forgejo   : {settings.Forgejo.ServiceURL}");
        WriteLine();
    }

    // 認証情報を準備
    WriteScriptTitle();
    WriteLine("サービス認証情報の準備");
    var forgejoToken = await settings.Forgejo.ApiTokenFile.BindTokenAsync("Forgejo APIトークン", settings.Forgejo.ServiceURL, signal.Token);
    Clear();
    WriteScriptTitle();

    // APIクライアントを準備
    WriteLine("Forgejo クライアントの生成 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var apiUser = await forgejo.User.GetMeAsync(cancelToken: signal.Token);
    WriteLine(Chalk.Gray[$"  .. User: {apiUser.login}"]);
    WriteLine();
    if (apiUser.is_admin != true) throw new PavedMessageException("APIトークンのユーザが管理者ではない。", PavedMessageKind.Warning);

    // リポジトリ設定更新
    await foreach (var repo in forgejo.AllReposAsync(signal.Token).WithCancellation(signal.Token))
    {
        Console.WriteLine($"Update: {repo.full_name}");
        await forgejo.Repository.UpdateAsync(repo.owner!.login!, repo.name!, settings.RepoSettings);
    }
});
