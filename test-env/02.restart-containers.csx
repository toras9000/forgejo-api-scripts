#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.102.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-interact"), async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);

    WriteLine("テスト環境の再起動 ...");
    var composeFile = ThisSource.RelativeFile("./docker/compose.yml");
    await "docker".args("compose", "--file", composeFile, "down", "--remove-orphans").result().success();
    await "docker".args("compose", "--file", composeFile, "up", "-d").result().success();
});
