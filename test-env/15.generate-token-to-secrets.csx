#!/usr/bin/env dotnet-script
#r "nuget: ForgejoApiClient, 13.0.0-rev.1"
#r "nuget: Lestaly.General, 0.112.0"
#r "nuget: Kokuban, 0.2.0"
#load "../.env-helper.csx"
#nullable enable
using ForgejoApiClient;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // サービスURL
    ServiceURL = new Uri("http://localhost:9940/"),

    // composeファイル
    ComposeFile = ThisSource.RelativeFile("./docker/compose.yml"),

    // forgejo コンテナサービス名
    ForgejoServiceName = "app",

    // APIキー保存ファイル
    AdminApiKeyFile = ThisSource.RelativeFile("../.admin-forgejo.key"),

    // APIキーの生成対象
    Targets = new[]
    {
        new
        {
            // トークン生成対象ユーザ名
            User = "toras9000",
            // トークン名
            TokenName = "action-package-write",
            // トークンスコープ
            TokenScope = "write:package,write:user,write:organization",
            // Secret名
            SecretName = "PACKAGE_WRITABLE_TOKEN",
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

    WriteLine("Forgejo クライアントの生成 ...");
    using var forgejo = new ForgejoClient(forgejoToken.Service, forgejoToken.Token);
    var apiUser = await forgejo.User.GetMeAsync(cancelToken: signal.Token);
    if (apiUser.login == null) throw new PavedMessageException("情報取得失敗", PavedMessageKind.Error);
    WriteLine(Chalk.Gray[$"  .. User: {apiUser.login}"]);
    WriteLine();

    WriteLine("テスト用 APIトークンの生成 ...");
    foreach (var target in settings.Targets)
    {
        WriteLine($"User: {target.User} - {target.TokenName}");
        var apiToken = await "docker".args("compose", "--file", settings.ComposeFile,
            "exec", "-u", "1000", settings.ForgejoServiceName,
            "forgejo", "admin", "user", "generate-access-token",
                "--username", target.User,
                "--token-name", target.TokenName,
                "--scopes", target.TokenScope,
                "--raw"
        ).silent().result().success().output(trim: true);
        WriteLine(".. 生成");

        WriteLine($".. Action Secret - {target.SecretName}");
        using var userClient = forgejo.Sudo(target.User);
        await userClient.User.SetActionsSecretAsync(target.SecretName, new(apiToken), signal.Token);
        WriteLine(".. 設定");
    }
});
