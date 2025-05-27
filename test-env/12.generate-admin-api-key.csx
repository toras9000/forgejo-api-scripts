#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.100.0"
#load "../.env-helper.csx"
#nullable enable
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // サービスのURL
    ServiceURL = new Uri("http://localhost:9940"),

    // composeファイル
    ComposeFile = ThisSource.RelativeFile("./docker/compose.yml"),

    // forgejo コンテナサービス名
    ForgejoServiceName = "app",

    // トークン生成対象ユーザ名
    TargetUser = "forgejo-admin",

    // トークン名
    TokenName = "test-token",

    // adminトークン保存ファイル
    ApiKeyFile = ThisSource.RelativeFile("../.admin-forgejo.key"),
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-interact"), async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("テスト用 APIトークンの生成 ...");
    var apiToken = await "docker".args("compose", "--file", settings.ComposeFile,
        "exec", "-u", "1000", settings.ForgejoServiceName,
        "forgejo", "admin", "user", "generate-access-token",
            "--username", settings.TargetUser,
            "--token-name", settings.TokenName,
            "--scopes", "all",
            "--raw"
    ).silent().result().success().output(trim: true);

    WriteLine("トークンをファイルに保存 ...");
    await settings.ApiKeyFile.ScriptScrambler().SaveTokenAsync(settings.ServiceURL, apiToken);

    WriteLine("完了");
});
