#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.106.0"
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

    // APIキーの生成対象
    Targets = new[]
    {
        new {
            // トークン生成対象ユーザ名
            User = "test-user",
            // トークン名
            TokenName = "test-token",
            // adminトークン保存ファイル
            ApiKeyFile = ThisSource.RelativeFile("../.tester-forgejo.key"),
        },
        new {
            // トークン生成対象ユーザ名
            User = "toras9000",
            // トークン名
            TokenName = "test-token",
            // adminトークン保存ファイル
            ApiKeyFile = ThisSource.RelativeFile("../.toras-forgejo.key"),
        },
    },
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-interact"), async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("テスト用 APIトークンの生成 ...");
    foreach (var target in settings.Targets)
    {
        WriteLine($"User: {target.User} - {target.TokenName}");
        var apiToken = await "docker".args("compose", "--file", settings.ComposeFile,
            "exec", "-u", "1000", settings.ForgejoServiceName,
            "forgejo", "admin", "user", "generate-access-token",
                "--username", target.User,
                "--token-name", target.TokenName,
                "--scopes", "all",
                "--raw"
        ).silent().result().success().output(trim: true);
        WriteLine(".. 生成");

        WriteLine($".. 保存 {target.ApiKeyFile.Name}");
        await target.ApiKeyFile.ScriptScrambler().SaveTokenAsync(settings.ServiceURL, apiToken);
    }
});
