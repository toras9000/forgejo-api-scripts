#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 13.0.0-rev.1"
#r "nuget: Kokuban, 0.2.0"
#r "nuget: Lestaly.General, 0.112.0"
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
    // サービスURL
    ServiceURL = new Uri("http://localhost:9940/"),

    // トークン保存ファイル
    AdminApiKeyFile = ThisSource.RelativeFile("../.admin-forgejo.key"),

    // クォータ設定
    Quota = new
    {
        // クォータグループ名
        GroupName = "default-quota",

        // クォータルール
        Rules = new CreateQuotaRuleOptions[]
        {
            new(name: "default-git-limit",          limit:  8 * 1024 * 1024 * 1024L, subjects: ["size:git:all"]),
            new(name: "default-repos-limit",        limit:  8 * 1024 * 1024 * 1024L, subjects: ["size:repos:all"]),
            new(name: "default-artifacts-limit",    limit:  2 * 1024 * 1024 * 1024L, subjects: ["size:assets:artifacts"]),
            new(name: "default-attachments-limit",  limit:  2 * 1024 * 1024 * 1024L, subjects: ["size:assets:attachments:all"]),
            new(name: "default-packages-limit",     limit: 16 * 1024 * 1024 * 1024L, subjects: ["size:assets:packages:all"]),
        },
    },
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-interact"), async () =>
{
    using var signal = new SignalCancellationPeriod();
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("APIトークンを読み込み ...");
    var forgejoToken = await settings.AdminApiKeyFile.ScriptScrambler().LoadTokenAsync(signal.Token) ?? throw new Exception("トークン情報を読み取れない");
    if (forgejoToken.Service.AbsoluteUri != settings.ServiceURL.AbsoluteUri) throw new Exception("保存情報が対象と合わない");

    WriteLine("クライアント準備 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);

    WriteLine("クォータグループの存在チェック");
    try
    {
        await forgejo.Admin.GetQuotaGroupAsync(settings.Quota.GroupName, signal.Token);
        WriteLine(".. 既に存在する");
        return;
    }
    catch { }

    WriteLine("クォータグループの作成");
    var options = new CreateQuotaGroupOptions(name: settings.Quota.GroupName, rules: settings.Quota.Rules);
    await forgejo.Admin.CreateQuotaGroupAsync(options, signal.Token);

    WriteLine(Chalk.Green[".. 完了"]);
});
