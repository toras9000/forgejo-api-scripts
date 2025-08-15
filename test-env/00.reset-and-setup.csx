#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.102.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    await "dotnet".args("script", ThisSource.RelativeFile("01.delete-containers.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("02.restart-containers.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("11.init-setup.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("12.generate-admin-api-key.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("13.create-users.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("14.generate-users-api-key.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("15.generate-token-to-secrets.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("19.create-default-quota-group.csx"), "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("21.setup-runner.csx"), "--", "--no-interact").echo().result().success();
});
