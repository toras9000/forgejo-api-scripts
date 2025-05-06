#r "nuget: Lestaly, 0.79.0"
#nullable enable
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    await "dotnet".args("script", ThisSource.RelativeFile("10.reset-restart.csx").FullName, "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("11.init-setup.csx").FullName, "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("12.generate-api-key.csx").FullName, "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("13.create-principals.csx").FullName, "--", "--no-interact").echo().result().success();
    await "dotnet".args("script", ThisSource.RelativeFile("14.create-repos.csx").FullName, "--", "--no-interact").echo().result().success();
});
